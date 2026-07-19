using Codify.Core.Interfaces;
using Codify.VisualStudio.Interfaces;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.VisualStudio.Services
{
    public sealed class WorkspaceFileService : IWorkspaceFileService
    {
        private readonly IFileSystem _fileSystem;

        public WorkspaceFileService(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public bool Exists(string filePath)
        {
            return _fileSystem.Exists(filePath);
        }

        public string ReadFile(string filePath)
        {
            if (!_fileSystem.Exists(filePath))
                throw new FileNotFoundException(filePath);

            return _fileSystem.ReadAllText(filePath);
        }
    }
}
