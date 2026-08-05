using System;
using System.Net;

namespace Codinex.Infrastructure.CustomeExceptions;
public sealed class OpenAiCompatibleException(
    HttpStatusCode statusCode,
    string responseBody,
    TimeSpan? retryAfter = null)
    : Exception(responseBody)
{
    public HttpStatusCode StatusCode { get; } = statusCode;

    public string ResponseBody { get; } = responseBody;

    public TimeSpan? RetryAfter { get; } = retryAfter;
}