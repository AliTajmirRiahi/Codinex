using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.VisualStudio.Interfaces
{
    public interface IWorkspaceFileService
    {
        string ReadFile(string filePath);

        bool Exists(string filePath);
    }
}
