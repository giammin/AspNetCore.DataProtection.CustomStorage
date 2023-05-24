namespace AspNetCore.DataProtection;

public class KeyInsertException:Exception
{
    public KeyInsertException(string message, Exception innerException):base(message, innerException)
    {
       
    }
}