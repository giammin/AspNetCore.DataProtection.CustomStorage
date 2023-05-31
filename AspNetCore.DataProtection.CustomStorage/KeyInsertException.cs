namespace AspNetCore.DataProtection.CustomStorage;

/// <summary>
/// exception raised when a new key cannot be inserted
/// </summary>
public class KeyInsertException:Exception
{
    /// <summary>
    /// init a new <see cref="KeyInsertException"/>
    /// </summary>
    /// <param name="message">the exception message</param>
    /// <param name="innerException">the inner exception</param>
    public KeyInsertException(string message, Exception innerException):base(message, innerException)
    {
       
    }
}