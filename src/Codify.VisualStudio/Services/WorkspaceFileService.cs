using Codify.Core.Interfaces;
using Codify.VisualStudio.Interfaces;
using Codify.VisualStudio.Models;
using Codify.VisualStudio.Models.Tools.ListDirectory;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Codify.VisualStudio.Extensions;

namespace Codify.VisualStudio.Services;

public sealed class WorkspaceFileService(IFileSystem fileSystem,
    IWorkspaceContext workspaceContext,
    IWorkspaceIgnoreService workspaceIgnoreService) : IWorkspaceFileService
{
    public bool Exists(string filePath)
    {
        return fileSystem.File.Exists(filePath);
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

        Write(filePath, content, encoding);

        return Task.CompletedTask;
    }

    public void Create(string filePath)
    {
        using (fileSystem.File.Create(filePath))
        {
        }
    }

    public Task CreateAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Create(filePath);

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

    public string GetRelativePath(string basePath, string path)
    {
        return PathExtensions.GetRelativePath(basePath, path);
    }
}
