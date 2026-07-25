
using System.Collections.Generic;

namespace Codify.Core.Interfaces
{
    public interface IFileSystem
    {
        bool Exists(string path);

        string ReadAllText(string path);

        bool FileExists(string path);

        bool DirectoryExists(string path);

        void CreateDirectory(string path);

        void DeleteFile(string path);

        void DeleteDirectory(string path, bool recursive);

        IEnumerable<string> GetDirectories(string path);

        IEnumerable<string> GetFiles(string path, string searchPattern = "");

        void WriteAllText(string path, string content);
    }
}
