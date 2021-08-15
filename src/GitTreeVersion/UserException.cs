using System;

namespace GitTreeVersion
{
    public class UserException : Exception
    {
        public UserException(string message) : base(message)
        {
        }
    }
}