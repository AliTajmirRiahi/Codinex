using Codinex.VisualStudio.Models;
using Codinex.VisualStudio.Models.Tools.ListDirectory;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
using Codinex.VisualStudio.Extensions;
using Codinex.VisualStudio.Interfaces;

namespace Codinex.VisualStudio.Services;

[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Foundation)]
public sealed class WorkspaceFileService(IFileSystem fileSystem,
    IWorkspaceContext workspaceContext,
    IWorkspaceIgnoreService workspaceIgnoreService) : IWorkspaceFileService
{
    public bool Exists(string path)
    {
        return fileSystem.File.Exists(path) || fileSystem.Directory.Exists(path);
    }

    public bool FileExists(string path)
    {
        return fileSystem.File.Exists(path);
    }

    public bool DirectoryExists(string path)
    {
        return fileSystem.Directory.Exists(path);
    }

    public string Read(string filePath)
    {
        return !fileSystem.File.Exists(filePath) ? throw new FileNotFoundException(filePath) : fileSystem.File.ReadAllText(filePath);
    }

    public Task<string> ReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(Read(filePath));
    }

    public void Write(string filePath, string content, Encoding encoding = null)
    {
        encoding ??= Encoding.UTF8;

        fileSystem.File.WriteAllText(filePath, content, encoding);
    }

    public Task WriteAsync(
        string filePath,
        string content,
        Encoding encoding = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Write(filePath, content, encoding ?? new UTF8Encoding(false));

        return Task.CompletedTask;
    }

    public void AppendText(
        string filePath,
        string content,
        Encoding encoding = null)
    {
        encoding ??= Encoding.UTF8;

        fileSystem.File.AppendAllText(
            filePath,
            content,
            encoding);
    }

    public Task AppendTextAsync(
        string filePath,
        string content,
        Encoding encoding = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        AppendText(
            filePath,
            content,
            encoding ?? new UTF8Encoding(false));

        return Task.CompletedTask;
    }

    public void CreateFile(string filePath)
    {
        using (fileSystem.File.Create(filePath))
        {
        }
    }

    public Task CreateFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        CreateFile(filePath);

        return Task.CompletedTask;
    }

    public void CreateDirectory(string filePath)
    {
        fileSystem.Directory.CreateDirectory(filePath);
    }

    public Task CreateDirectoryAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        CreateDirectory(filePath);

        return Task.CompletedTask;
    }

    public void Delete(string filePath)
    {
        fileSystem.File.Delete(filePath);
    }

    public Task DeleteAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Delete(filePath);

        return Task.CompletedTask;
    }

    public void Copy(string sourcePath, string destinationPath, bool overwrite = false)
    {
        fileSystem.File.Copy(sourcePath, destinationPath, overwrite);
    }

    public void Move(string sourcePath, string destinationPath, bool overwrite = false)
    {
        if (overwrite && fileSystem.File.Exists(destinationPath))
        {
            fileSystem.File.Delete(destinationPath);
        }

        if (destinationPath != null) fileSystem.File.Move(sourcePath, destinationPath);
    }

    public long GetSize(string filePath)
    {
        return fileSystem.FileInfo.New(filePath).Length;
    }

    public DateTime GetLastWriteTime(string filePath)
    {
        return fileSystem.File.GetLastWriteTime(filePath);
    }

    public IEnumerable<string> EnumerateFiles(
        string directory,
        string searchPattern = "*",
        SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        return fileSystem.Directory.EnumerateFiles(
            directory,
            searchPattern,
            searchOption);
    }

    public IEnumerable<string> EnumerateDirectories(
        string directory,
        SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        return fileSystem.Directory.EnumerateDirectories(
            directory,
            "*",
            searchOption);
    }

    public Stream OpenRead(string filePath)
    {
        return fileSystem.File.OpenRead(filePath);
    }

    // Determines whether the specified file is binary.
    public bool IsBinary(string filePath)
    {
        if (!Exists(filePath))
            return false;

        const int sampleSize = 4096;

        byte[] buffer;

        using (var stream = OpenRead(filePath))
        {
            buffer = new byte[Math.Min(sampleSize, (int)stream.Length)];

            var bytesRead = stream.Read(buffer, 0, buffer.Length);

            if (bytesRead != buffer.Length)
                Array.Resize(ref buffer, bytesRead);
        }

        if (buffer.Length == 0)
            return false;

        // A null byte is a strong indicator of binary content.
        if (buffer.Any(b => b == 0))
            return true;

        var controlCount = 0;

        foreach (var b in buffer)
        {
            switch (b)
            {
                case (byte)'\r':
                case (byte)'\n':
                case (byte)'\t':
                    continue;
            }

            if (b < 32)
                controlCount++;
        }

        return (controlCount * 100.0 / buffer.Length) > 10;
    }

    public async Task<IReadOnlyList<WorkspaceEntry>> ListDirectoryAsync(
        string relativePath,
        CancellationToken cancellationToken)
    {
        await Task.Yield();

        cancellationToken.ThrowIfCancellationRequested();

        var directoryPath = string.IsNullOrWhiteSpace(relativePath)
            ? workspaceContext.SolutionDirectory
            : Path.Combine(workspaceContext.SolutionDirectory, relativePath);

        if (!fileSystem.Directory.Exists(directoryPath))
        {
            return [];
        }

        var entries = fileSystem.Directory
            .EnumerateFileSystemEntries(directoryPath)
            .Where(path => !workspaceIgnoreService.ShouldIgnore(path))
            .Select(path => new WorkspaceEntry
            {
                Name = fileSystem.Path.GetFileName(path),
                FullPath = path,
                RelativePath = GetRelativePath(workspaceContext.SolutionDirectory, path),
                Type = fileSystem.Directory.Exists(path)
                    ? WorkspaceEntryType.Directory
                    : WorkspaceEntryType.File
            })
            .OrderByDescending(e => e.Type == WorkspaceEntryType.Directory)
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return entries;
    }


    public Task CopyAsync(
        string sourcePath,
        string destinationPath,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            Copy(sourcePath, destinationPath, overwrite);
        }, cancellationToken);
    }

    public Task RenameAsync(
        string filePath,
        string newFileName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        if (string.IsNullOrWhiteSpace(newFileName))
        {
            throw new ArgumentException("New file name cannot be null or empty.", nameof(newFileName));
        }

        if (!fileSystem.File.Exists(filePath))
        {
            throw new FileNotFoundException("The source file was not found.", filePath);
        }

        var directory = fileSystem.Path.GetDirectoryName(filePath)!;
        var destinationPath = fileSystem.Path.Combine(directory, newFileName);

        if (fileSystem.File.Exists(destinationPath))
        {
            throw new IOException($"The destination file '{destinationPath}' already exists.");
        }

        fileSystem.File.Move(filePath, destinationPath);

        return Task.CompletedTask;
    }

    public Task MoveAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("Source path cannot be null or empty.", nameof(sourcePath));
        }

        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            throw new ArgumentException("Destination path cannot be null or empty.", nameof(destinationPath));
        }

        if (!fileSystem.File.Exists(sourcePath))
        {
            throw new FileNotFoundException("The source file was not found.", sourcePath);
        }

        if (fileSystem.File.Exists(destinationPath))
        {
            throw new IOException($"The destination file '{destinationPath}' already exists.");
        }

        var destinationDirectory = fileSystem.Path.GetDirectoryName(destinationPath)!;

        if (!fileSystem.Directory.Exists(destinationDirectory))
        {
            fileSystem.Directory.CreateDirectory(destinationDirectory);
        }

        fileSystem.File.Move(sourcePath, destinationPath);

        return Task.CompletedTask;
    }

    public string GetRelativePath(string basePath, string path)
    {
        return PathExtensions.GetRelativePath(basePath, path);
    }
}
