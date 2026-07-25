using System;
using System.Collections.Generic;
using System.IO;
using Codify.Core.Interfaces;

namespace Codify.Infrastructure.IO
{
    public sealed class FileSystem : IFileSystem
    {
        public bool Exists(string path)
        {
            return File.Exists(path) || Directory.Exists(path);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }

        public void WriteAllText(string path, string content)
        {
            var directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, content);
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public void DeleteFile(string path)
        {
            File.Delete(path);
        }

        public void DeleteDirectory(string path, bool recursive)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive);
            }
        }

        public IEnumerable<string> GetDirectories(string path)
        {
            return !Directory.Exists(path) ? [] : Directory.GetDirectories(path);
        }

        public IEnumerable<string> GetFiles(string path, string searchPattern = "")
        {
            return !Directory.Exists(path) ? [] : Directory.GetFiles(path, searchPattern);
        }
    }
}