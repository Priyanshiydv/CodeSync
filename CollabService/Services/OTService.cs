using CollabService.Models;

namespace CollabService.Services
{
    /// <summary>
    /// Implements server-side Operational Transformation.
    /// Transforms incoming operations against concurrent operations
    /// to ensure all clients converge to the same document state.
    /// Case study §2.6 — OT applied server-side before SignalR broadcast.
    /// </summary>
    public class OTService
    {
        // In-memory store of operations per session
        // key = sessionId, value = list of applied operations
        private readonly Dictionary<string,
            List<EditOperation>> _sessionHistory = new();

        private readonly object _lock = new();

        /// <summary>
        /// Transforms and applies an incoming operation.
        /// Returns the transformed operation ready to broadcast.
        /// </summary>
        public EditOperation Transform(
            string sessionId, EditOperation incoming)
        {
            lock (_lock)
            {
                // Get history for this session
                if (!_sessionHistory.ContainsKey(sessionId))
                    _sessionHistory[sessionId] = new();

                var history = _sessionHistory[sessionId];

                // Get operations the client hasn't seen yet
                // (operations with revision > client's revision)
                var concurrentOps = history
                    .Where(op => op.Revision > incoming.Revision)
                    .ToList();

                // Transform incoming operation against
                // each concurrent operation
                var transformed = incoming;
                foreach (var concurrent in concurrentOps)
                {
                    transformed = TransformAgainst(
                        transformed, concurrent);
                }

                // Assign new revision number
                transformed.Revision = history.Count + 1;

                // Store in history
                history.Add(transformed);

                return transformed;
            }
        }

        /// <summary>
        /// Transforms operation A against concurrent operation B.
        /// Adjusts position of A based on what B did to the document.
        /// </summary>
        private EditOperation TransformAgainst(
            EditOperation a, EditOperation b)
        {
            // Only position needs adjusting — clone first
            var result = new EditOperation
            {
                Type = a.Type,
                Position = a.Position,
                Text = a.Text,
                Length = a.Length,
                UserId = a.UserId,
                Revision = a.Revision
            };

            if (b.Type == "insert")
            {
                // B inserted text before A's position
                // shift A's position right by inserted length
                if (b.Position <= result.Position)
                    result.Position += b.Text?.Length ?? 0;
            }
            else if (b.Type == "delete")
            {
                // B deleted text before A's position
                // shift A's position left by deleted length
                if (b.Position < result.Position)
                {
                    var overlap = Math.Min(
                        b.Length,
                        result.Position - b.Position);
                    result.Position -= overlap;
                }
                // If B deleted at same position as A
                // and A is also a delete — adjust length
                else if (b.Position == result.Position
                    && result.Type == "delete")
                {
                    result.Length = Math.Max(
                        0, result.Length - b.Length);
                }
            }

            return result;
        }

        /// <summary>
        /// Applies a transformed operation to a document string.
        /// Returns the new document content.
        /// </summary>
        public string ApplyOperation(
            string document, EditOperation op)
        {
            try
            {
                switch (op.Type)
                {
                    case "insert":
                        if (op.Position < 0 ||
                            op.Position > document.Length)
                            return document;
                        return document.Insert(
                            op.Position, op.Text ?? "");

                    case "delete":
                        if (op.Position < 0 ||
                            op.Position >= document.Length)
                            return document;
                        var safeLength = Math.Min(
                            op.Length,
                            document.Length - op.Position);
                        return document.Remove(
                            op.Position, safeLength);

                    default:
                        // "full" or "retain" — content is the full doc
                        return document;
                }
            }
            catch
            {
                // If OT fails — return document unchanged
                return document;
            }
        }

        /// <summary>
        /// Clears session history when session ends.
        /// Prevents memory leaks.
        /// </summary>
        public void ClearSession(string sessionId)
        {
            lock (_lock)
            {
                _sessionHistory.Remove(sessionId);
            }
        }

        /// <summary>
        /// Returns current revision number for a session.
        /// </summary>
        public int GetRevision(string sessionId)
        {
            lock (_lock)
            {
                return _sessionHistory.ContainsKey(sessionId)
                    ? _sessionHistory[sessionId].Count
                    : 0;
            }
        }
    }
}