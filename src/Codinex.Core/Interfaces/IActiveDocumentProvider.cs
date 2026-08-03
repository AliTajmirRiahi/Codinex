using System.Threading.Tasks;
using Codinex.Core.Models;

namespace Codinex.Core.Interfaces
{

    public interface IActiveDocumentProvider
    {
        Task<ReferenceItem> GetActiveDocumentAsync();
        Task<ReferenceItem> GetActiveDocumentAsync(string filePath);
    }
}
