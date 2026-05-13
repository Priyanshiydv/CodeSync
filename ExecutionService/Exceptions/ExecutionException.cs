namespace ExecutionService.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }

    public class LanguageNotSupportedException : Exception
    {
        public LanguageNotSupportedException(string message) : base(message) { }
    }

    public class JobCancelException : Exception
    {
        public JobCancelException(string message) : base(message) { }
    }
}