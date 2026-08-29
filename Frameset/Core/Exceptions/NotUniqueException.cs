using System;

namespace Frameset.Core.Exceptions;

public class NotUniqueException : Exception
{
    public NotUniqueException(string message) : base(message)
    {

    }
    public NotUniqueException(string message, Exception innerClass) : base(message, innerClass)
    {

    }
    
}