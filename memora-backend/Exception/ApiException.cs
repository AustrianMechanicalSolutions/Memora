public class ApiException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }

    public ApiException(string code, string message, int statusCode = 400)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }
}