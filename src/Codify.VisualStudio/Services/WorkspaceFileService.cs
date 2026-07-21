using Codify.Core.Interfaces;
using Codify.VisualStudio.Interfaces;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.VisualStudio.Services
{
    public sealed class WorkspaceFileService(IFileSystem fileSystem) : IWorkspaceFileService
    {
        public bool Exists(string filePath)
        {
            return fileSystem.Exists(filePath);
        }

        public string ReadFile(string filePath)
        {
            if (!fileSystem.Exists(filePath))
                throw new FileNotFoundException(filePath);

            return fileSystem.ReadAllText(filePath);
        }
    }
}
