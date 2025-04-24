using System;

namespace TodoApp.Domain.Exceptions;

public class TodoDomainException : Exception
{
    public TodoDomainException()
    {
    }

    public TodoDomainException(string message)
        : base(message)
    {
    }

    public TodoDomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
