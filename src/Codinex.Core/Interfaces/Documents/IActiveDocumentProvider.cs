using System.Threading.Tasks;
using Codinex.Core.Models.References;

namespace Codinex.Core.Interfaces.Documents
{

    public interface IActiveDocumentProvider
    {
        Task<ReferenceItem> GetActiveDocumentAsync();
        Task<ReferenceItem> GetActiveDocumentAsync(string filePath);
    }
}
