using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.Core.Models;
using Codinex.Infrastructure.CustomeExceptions;
using Newtonsoft.Json.Linq;

namespace Codinex.Infrastructure.AI.Errors
{
    internal static class AiErrorFactory
    {
        public static bool TryCreateExpected(
            Exception exception,
            CancellationToken cancellationToken,
            out AiError error)
        {
            error = null;

            switch (exception)
            {
                case OpenAiCompatibleException openAiException:
                    error = FromHttpStatusCode(
                        openAiException.StatusCode,
                        openAiException.ResponseBody,
                        openAiException.RetryAfter,
                        openAiException);
                    return true;

                case OperationCanceledException cancellationException when cancellationToken.IsCancellationRequested:
                    error = Create(
                        AiErrorCode.Cancelled,
                        "The AI request was cancelled.",
                        false,
                        exception: cancellationException);
                    return true;

                case TimeoutException timeoutException:
                    error = Create(
                        AiErrorCode.Timeout,
                        "The AI provider did not respond in time.",
                        true,
                        exception: timeoutException);
                    return true;

                case TaskCanceledException timeoutException when !cancellationToken.IsCancellationRequested:
                    error = Create(
                        AiErrorCode.Timeout,
                        "The AI provider request timed out.",
                        true,
                        exception: timeoutException);
                    return true;

                case HttpRequestException httpException:
                    error = Create(
                        AiErrorCode.Network,
                        "A network error occurred while contacting the AI provider.",
                        true,
                        exception: httpException);
                    return true;

                case SocketException socketException:
                    error = Create(
                        AiErrorCode.Network,
                        "A network error occurred while contacting the AI provider.",
                        true,
                        exception: socketException);
                    return true;

                case IOException ioException:
                    error = Create(
                        AiErrorCode.Network,
                        "The AI provider connection was interrupted.",
                        true,
                        exception: ioException);
                    return true;

                case WebException webException:
                    error = Create(
                        AiErrorCode.Network,
                        "A network error occurred while contacting the AI provider.",
                        true,
                        exception: webException);
                    return true;

                default:
                    return false;
            }
        }

        public static AiError FromProviderErrorBody(
            string responseBody,
            Exception exception = null)
        {
            var code = ClassifyByBody(responseBody);

            return Create(
                code,
                BuildMessage(code, ExtractProviderMessage(responseBody)),
                IsRetryable(code),
                exception: exception);
        }

        public static AiError FromHttpStatusCode(
            HttpStatusCode statusCode,
            string responseBody,
            TimeSpan? retryAfter = null,
            Exception exception = null)
        {
            var code = MapStatusCode(statusCode, responseBody);

            return Create(
                code,
                BuildMessage(code, ExtractProviderMessage(responseBody)),
                IsRetryable(code),
                retryAfter,
                exception);
        }

        public static ConversationEvent ToConversationEvent(AiError error)
        {
            return ConversationEvent.Create(
                ConversationEventType.ConversationFailed,
                JToken.FromObject(error),
                error.Message);
        }

        private static AiError Create(
            AiErrorCode code,
            string message,
            bool isRetryable,
            TimeSpan? retryAfter = null,
            Exception exception = null)
        {
            return new AiError(
                code,
                message,
                isRetryable,
                retryAfter,
                exception);
        }

        private static AiErrorCode MapStatusCode(
            HttpStatusCode statusCode,
            string responseBody)
        {
            var status = (int)statusCode;
            var bodyCode = ClassifyByBody(responseBody);

            if (bodyCode != AiErrorCode.Unknown)
            {
                return bodyCode;
            }

            switch (status)
            {
                case 400:
                    return AiErrorCode.Unknown;
                case 401:
                    return AiErrorCode.AuthenticationFailed;
                case 402:
                    return AiErrorCode.InsufficientCredits;
                case 403:
                    return AiErrorCode.AuthenticationFailed;
                case 404:
                    return AiErrorCode.ModelNotFound;
                case 408:
                    return AiErrorCode.Timeout;
                case 413:
                    return AiErrorCode.ContextLengthExceeded;
                case 429:
                    return AiErrorCode.RateLimitExceeded;
                case 499:
                    return AiErrorCode.Cancelled;
                default:
                    return status >= 500 && status <= 599
                        ? AiErrorCode.ProviderUnavailable
                        : AiErrorCode.Unknown;
            }
        }

        private static AiErrorCode ClassifyByBody(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return AiErrorCode.Unknown;
            }

            var text = responseBody.ToLowerInvariant();

            if (ContainsAny(text, "invalid_api_key", "invalid api key", "incorrect api key", "api key is invalid"))
            {
                return AiErrorCode.InvalidApiKey;
            }

            if (ContainsAny(text, "unauthorized", "authentication", "permission denied", "forbidden"))
            {
                return AiErrorCode.AuthenticationFailed;
            }

            if (ContainsAny(text, "unsupported region", "region is not supported", "not available in your region", "unsupported_country_region_territory"))
            {
                return AiErrorCode.UnsupportedRegion;
            }

            if (ContainsAny(text, "insufficient credits", "insufficient quota", "insufficient_quota", "quota exceeded", "billing", "payment required", "credit balance"))
            {
                return AiErrorCode.InsufficientCredits;
            }

            if (ContainsAny(text, "rate limit", "too many requests", "ratelimit"))
            {
                return AiErrorCode.RateLimitExceeded;
            }

            if (ContainsAny(text, "context length", "maximum context", "token limit", "too many tokens", "context_length_exceeded"))
            {
                return AiErrorCode.ContextLengthExceeded;
            }

            if (ContainsAny(text, "model not found", "does not exist", "unknown model", "model_not_found"))
            {
                return AiErrorCode.ModelNotFound;
            }

            if (ContainsAny(text, "provider unavailable", "service unavailable", "temporarily unavailable", "overloaded", "bad gateway", "gateway timeout"))
            {
                return AiErrorCode.ProviderUnavailable;
            }

            if (ContainsAny(text, "content filtered", "content_filter", "safety", "policy violation", "moderation"))
            {
                return AiErrorCode.ContentFiltered;
            }

            if (ContainsAny(text, "cancelled", "canceled"))
            {
                return AiErrorCode.Cancelled;
            }

            if (ContainsAny(text, "timeout", "timed out"))
            {
                return AiErrorCode.Timeout;
            }

            return AiErrorCode.Unknown;
        }

        private static bool IsRetryable(AiErrorCode code)
        {
            switch (code)
            {
                case AiErrorCode.Network:
                case AiErrorCode.Timeout:
                case AiErrorCode.RateLimitExceeded:
                case AiErrorCode.ProviderUnavailable:
                    return true;
                default:
                    return false;
            }
        }

        private static string BuildMessage(
            AiErrorCode code,
            string providerMessage)
        {
            var message = string.IsNullOrWhiteSpace(providerMessage)
                ? GetDefaultMessage(code)
                : providerMessage.Trim();

            return message.Length <= 1000
                ? message
                : message.Substring(0, 1000) + "...";
        }

        private static string GetDefaultMessage(AiErrorCode code)
        {
            switch (code)
            {
                case AiErrorCode.Network:
                    return "A network error occurred while contacting the AI provider.";
                case AiErrorCode.Timeout:
                    return "The AI provider request timed out.";
                case AiErrorCode.AuthenticationFailed:
                    return "Authentication with the AI provider failed.";
                case AiErrorCode.InvalidApiKey:
                    return "The configured AI provider API key is invalid.";
                case AiErrorCode.UnsupportedRegion:
                    return "The AI provider is not available in the configured region.";
                case AiErrorCode.InsufficientCredits:
                    return "The AI provider account has insufficient credits or quota.";
                case AiErrorCode.RateLimitExceeded:
                    return "The AI provider rate limit was exceeded.";
                case AiErrorCode.ContextLengthExceeded:
                    return "The request exceeds the AI model context length.";
                case AiErrorCode.ModelNotFound:
                    return "The selected AI model was not found by the provider.";
                case AiErrorCode.ProviderUnavailable:
                    return "The AI provider is currently unavailable.";
                case AiErrorCode.ContentFiltered:
                    return "The AI provider filtered the request or response content.";
                case AiErrorCode.Cancelled:
                    return "The AI request was cancelled.";
                default:
                    return "The AI provider returned an error.";
            }
        }

        private static string ExtractProviderMessage(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return string.Empty;
            }

            try
            {
                var json = JObject.Parse(responseBody);
                var error = json["error"];

                if (error is JObject errorObject)
                {
                    return errorObject["message"]?.ToString()
                           ?? errorObject["code"]?.ToString()
                           ?? responseBody;
                }

                return error?.ToString()
                       ?? json["message"]?.ToString()
                       ?? responseBody;
            }
            catch
            {
                return responseBody;
            }
        }

        private static bool ContainsAny(
            string value,
            params string[] fragments)
        {
            foreach (var fragment in fragments)
            {
                if (value.Contains(fragment))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
