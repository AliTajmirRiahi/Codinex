using System.Threading.Tasks;
using Codify.Core.Models;

namespace Codify.Core.Abstractions
{

    public interface IActiveDocumentProvider
    {
        Task<ReferenceItem> GetActiveDocumentAsync();
        Task<ReferenceItem> GetActiveDocumentAsync(string filePath);
    }
}
