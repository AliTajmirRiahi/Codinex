using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Core.Interfaces
{
    public interface IHttpService
    {
        Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken = default);
    }
}
