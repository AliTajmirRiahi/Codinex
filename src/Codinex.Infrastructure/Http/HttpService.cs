using System;
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

        using var handler = CreateHandler();

        using var client = CreateClient(handler);

        return await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
    }
    private static HttpClientHandler CreateHandler()
    {
        return new HttpClientHandler
        {
            AutomaticDecompression =
                DecompressionMethods.GZip |
                DecompressionMethods.Deflate
        };
    }

    private static HttpClient CreateClient(
        HttpClientHandler handler)
    {
        return new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

}