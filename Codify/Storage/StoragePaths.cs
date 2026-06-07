using System;
using System.IO;

namespace Codify.Storage
{
    /// <summary>
    /// Central place for all Codify storage directories.
    /// </summary>
    public static class StoragePaths
    {
        public static readonly string Root =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Codify"
            );

        public static readonly string Chats =
            Path.Combine(Root, "chats");

        public static readonly string Cache =
            Path.Combine(Root, "cache");

        public static readonly string Sessions =
            Path.Combine(Root, "sessions");

        public static readonly string Settings =
            Path.Combine(Root, "settings.json");

        public static readonly string Providers =
            Path.Combine(Root, "providers.json");

        /// <summary>
        /// Ensure all required directories exist.
        /// </summary>
        public static void EnsureCreated()
        {
            Directory.CreateDirectory(Root);
            Directory.CreateDirectory(Chats);
            Directory.CreateDirectory(Cache);
            Directory.CreateDirectory(Sessions);
        }
    }
}