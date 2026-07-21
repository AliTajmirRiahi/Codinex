using System;
using System.Net;

namespace Codify.Infrastructure.CustomeExceptions;
public sealed class OpenAiCompatibleException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public string ResponseBody { get; }

    public OpenAiCompatibleException(
        HttpStatusCode statusCode,
        string responseBody)
        : base(responseBody)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}