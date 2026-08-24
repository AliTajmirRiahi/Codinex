using System;
using System.IO;

namespace Codinex.Storage.Services
{
    /// <summary>
    /// Provides centralized access to all Codinex storage paths.
    /// </summary>
    public static class StoragePaths
    {
        #region Root

        public static string Root =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Codinex");

        #endregion

        #region Global

        public static string Cache =>
            Path.Combine(Root, "cache");

        public static string Sessions =>
            Path.Combine(Root, "sessions");

        public static string Providers =>
            Path.Combine(Root, "providers.json");

        public static string Settings =>
            Path.Combine(Root, "settings.json");

        #endregion

        #region Workspaces

        public static string Workspaces =>
            Path.Combine(Root, "workspaces");

        public static string GetWorkspacePath(string workspaceName) =>
            Path.Combine(Workspaces, workspaceName);

        public static string GetWorkspaceMemoryPath(string workspaceName) =>
            Path.Combine(GetWorkspacePath(workspaceName), "memory.json");

        public static string GetWorkspaceSettingsPath(string workspaceName) =>
            Path.Combine(GetWorkspacePath(workspaceName), "settings.json");

        public static string GetPendingChangesetPath(string workspaceName) =>
            Path.Combine(GetWorkspacePath(workspaceName), "pending-changeset.json");

        #endregion

        #region Groups

        public static string GetGroupsPath(string workspaceName) =>
            Path.Combine(GetWorkspacePath(workspaceName), "groups");

        public static string GetGroupPath(
            string workspaceName,
            string groupId) =>
            Path.Combine(GetGroupsPath(workspaceName), groupId);

        public static string GetGroupJsonPath(
            string workspaceName,
            string groupId) =>
            Path.Combine(GetGroupPath(workspaceName,groupId), "group.json");

        public static string GetChatPath(
            string workspaceName,
            string groupId,
            string chatId) =>
            Path.Combine(
                GetGroupPath(workspaceName, groupId),
                $"{chatId}.json");

        #endregion

        #region Prompts

        public static string Prompts =>
            Path.Combine(Root, "prompts");

        public static string GetChatPromptsPath(string chatId) =>
            Path.Combine(Prompts, $"chat_{chatId}");

        public static string GetChatMessagePromptsPath(string chatId, string chatMessageId) =>
            Path.Combine(GetChatPromptsPath(chatId), chatMessageId);

        #endregion
    }
}