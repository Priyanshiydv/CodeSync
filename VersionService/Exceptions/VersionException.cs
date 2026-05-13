namespace VersionService.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }

    public class IntegrityException : Exception
    {
        public IntegrityException(string message) : base(message) { }
    }

    public class AlreadyExistsException : Exception
    {
        public AlreadyExistsException(string message) : base(message) { }
    }
}