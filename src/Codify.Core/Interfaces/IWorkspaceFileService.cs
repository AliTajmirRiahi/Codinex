using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models;

namespace Codify.Core.Interfaces
{
    public interface IWorkspaceFileService
    {
        // Read

        string Read(string filePath);

        Task<string> ReadAsync(string filePath, CancellationToken cancellationToken = default);

        // Write

        void Write(string filePath, string content, Encoding encoding = null);

        Task WriteAsync(
            string filePath,
            string content,
            Encoding encoding = null,
            CancellationToken cancellationToken = default);

        // Create

        void Create(string filePath);

        Task CreateAsync(string filePath, CancellationToken cancellationToken = default);

        // Delete

        void Delete(string filePath);

        Task DeleteAsync(string filePath, CancellationToken cancellationToken = default);

        // Copy / Move

        void Copy(string sourcePath, string destinationPath, bool overwrite = false);

        void Move(string sourcePath, string destinationPath, bool overwrite = false);

        // Query

        bool Exists(string filePath);

        bool FileExists(string path);
        bool DirectoryExists(string path);

        long GetSize(string filePath);

        DateTime GetLastWriteTime(string filePath);

        // Enumerate

        IEnumerable<string> EnumerateFiles(
            string directory,
            string searchPattern = "*",
            SearchOption searchOption = SearchOption.TopDirectoryOnly);

        IEnumerable<string> EnumerateDirectories(
            string directory,
            SearchOption searchOption = SearchOption.TopDirectoryOnly);

        Stream OpenRead(string filePath);

        bool IsBinary(string filePath);

        Task<IReadOnlyList<WorkspaceEntry>> ListDirectoryAsync(
            string path,
            CancellationToken cancellationToken);

        Task CopyAsync(
            string sourcePath,
            string destinationPath,
            bool overwrite = false,
            CancellationToken cancellationToken = default);

        Task RenameAsync(
            string filePath,
            string newFileName,
            CancellationToken cancellationToken = default);

        Task MoveAsync(
            string sourcePath,
            string destinationPath,
            CancellationToken cancellationToken = default);
    }
}
