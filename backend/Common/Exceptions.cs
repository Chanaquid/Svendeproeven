namespace backend.Common
{
    public class UnauthorizedAppException : Exception
    {
        public UnauthorizedAppException(string message)
            : base(message)
        {
        }
    }

    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message)
            : base(message)
        {
        }
    }


}
