namespace CollabService.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }

    public class AlreadyExistsException : Exception
    {
        public AlreadyExistsException(string message) : base(message) { }
    }

    public class SessionEndedException : Exception
    {
        public SessionEndedException(string message) : base(message) { }
    }

    public class SessionFullException : Exception
    {
        public SessionFullException(string message) : base(message) { }
    }
}