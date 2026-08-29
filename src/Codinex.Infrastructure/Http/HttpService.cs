using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Services;
namespace Codinex.Infrastructure.Http;

[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Infrastructure)]
internal sealed class HttpService : IHttpService
{
    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // A fresh client/handler per request so nothing is shared across calls
        // (a single shared HttpClient on .NET Framework funnels every provider
        // through one ServicePoint and stalls once DefaultConnectionLimit
        // streaming requests are in flight).
        //
        // The client and handler must NOT be disposed when this method returns:
        // with HttpCompletionOption.ResponseHeadersRead the response body is still
        // being read off the connection by the caller, and tearing the handler
        // down first leaves that stream unreadable ("Stream was not readable" in
        // StreamReader). Instead their lifetime is bound to the response: every
        // caller wraps it in `using`, so disposing the response - after the body
        // has been fully consumed - disposes the client and handler too.
        var handler = CreateHandler();
        var client = CreateClient(handler);

        HttpResponseMessage response = null;

        try
        {
            response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.Content = new OwnerBoundContent(response.Content, client);

            return response;
        }
        catch
        {
            response?.Dispose();
            client.Dispose();
            handler.Dispose();

            throw;
        }
    }

    private static HttpClientHandler CreateHandler()
    {
        return new HttpClientHandler
        {
            AutomaticDecompression =
                DecompressionMethods.GZip |
                DecompressionMethods.Deflate,
            MaxConnectionsPerServer = 20
        };
    }

    private static HttpClient CreateClient(
        HttpClientHandler handler)
    {
        // disposeHandler: true - disposing the client disposes the handler.
        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    /// <summary>
    /// Delegates to the real response content but also disposes the owning
    /// <see cref="HttpClient"/> (and, through it, the handler) when the content
    /// - and therefore the <see cref="HttpResponseMessage"/> - is disposed.
    /// </summary>
    private sealed class OwnerBoundContent : HttpContent
    {
        private readonly HttpContent inner;
        private readonly IDisposable owner;

        public OwnerBoundContent(HttpContent inner, IDisposable owner)
        {
            this.inner = inner;
            this.owner = owner;

            foreach (var header in inner.Headers)
                Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            => inner.CopyToAsync(stream, context);

        protected override Task<Stream> CreateContentReadStreamAsync()
            => inner.ReadAsStreamAsync();

        protected override bool TryComputeLength(out long length)
        {
            length = inner.Headers.ContentLength ?? -1L;

            return length >= 0L;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
                owner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
