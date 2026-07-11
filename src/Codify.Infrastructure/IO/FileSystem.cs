using Codify.Core.Interfaces;
using System.IO;

namespace Codify.Infrastructure.IO
{
    public sealed class FileSystem : IFileSystem
    {
        public bool Exists(string path) => File.Exists(path);

        public string ReadAllText(string path) => File.ReadAllText(path);
    }
}
