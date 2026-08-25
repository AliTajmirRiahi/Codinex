using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Git;
using Codinex.Core.Interfaces.Workspace;
using Codinex.Core.Models.Git;
using Codinex.Storage.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Codinex.Infrastructure.BugReporting
{
    [AutoDiRegister(Modules.AI, RegistrationOrder.Infrastructure)]
    public sealed class BugReportService(
        IWorkspaceFileService workspaceFileService,
        IGitHubIssueService gitHubIssueService) : IBugReportService
    {
        // GitHub payloads have a practical ceiling too; keep the output log well under
        // that so the issue body never gets rejected outright.
        private const int MaxOutputLogLength = 800_000;

        public async Task<BugReportResult> SubmitAsync(
            string chatId,
            string description,
            string outputLog,
            IReadOnlyDictionary<string, string> vsInfo,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return BugReportResult.Failed("A description of the bug is required.");
            }

            var systemInfo = CollectSystemInfo(vsInfo);
            var lastPrompt = CollectLastPrompt(chatId);
            var truncatedOutputLog = Truncate(outputLog);

            var issueResult = await gitHubIssueService.CreateIssueAsync(
                BuildIssueTitle(description),
                BuildIssueBody(chatId, description, systemInfo, lastPrompt, truncatedOutputLog),
                cancellationToken);

            return issueResult.Success
                ? BugReportResult.Ok($"Bug report filed as a GitHub issue")
                : BugReportResult.Failed($"Failed to file GitHub issue");
        }

        private static string BuildIssueTitle(string description)
        {
            var firstLine = description.Split('\n')[0].Trim();

            return firstLine.Length <= 80
                ? $"[Bug] {firstLine}"
                : $"[Bug] {firstLine.Substring(0, 80)}…";
        }

        private static string BuildIssueBody(
            string chatId,
            string description,
            Dictionary<string, object> systemInfo,
            object lastPrompt,
            string truncatedOutputLog)
        {
            var body = new StringBuilder();

            body.AppendLine("## Description");
            body.AppendLine(description);
            body.AppendLine();

            body.AppendLine("## System Info");
            body.AppendLine("```json");
            body.AppendLine(JsonConvert.SerializeObject(systemInfo, Formatting.Indented));
            body.AppendLine("```");
            body.AppendLine();

            body.AppendLine("<details><summary>Codinex output log</summary>");
            body.AppendLine();
            body.AppendLine("```");
            body.AppendLine(string.IsNullOrWhiteSpace(truncatedOutputLog) ? "(empty)" : truncatedOutputLog);
            body.AppendLine("```");
            body.AppendLine("</details>");
            body.AppendLine();

            body.AppendLine("<details><summary>Last chat prompt</summary>");
            body.AppendLine();
            body.AppendLine("```json");
            body.AppendLine(lastPrompt == null
                ? "(none recorded)"
                : JsonConvert.SerializeObject(lastPrompt, Formatting.Indented));
            body.AppendLine("```");
            body.AppendLine("</details>");
            body.AppendLine();

            body.AppendLine($"_Chat ID: {chatId}_");

            return body.ToString();
        }

        private static string Truncate(string outputLog)
        {
            if (string.IsNullOrEmpty(outputLog))
            {
                return outputLog;
            }

            return outputLog.Length <= MaxOutputLogLength
                ? outputLog
                : outputLog.Substring(outputLog.Length - MaxOutputLogLength);
        }

        private static Dictionary<string, object> CollectSystemInfo(IReadOnlyDictionary<string, string> vsInfo)
        {
            var info = new Dictionary<string, object>
            {
                ["OsVersion"] = Environment.OSVersion.ToString(),
                ["Framework"] = RuntimeInformation.FrameworkDescription,
                ["ClrVersion"] = Environment.Version.ToString(),
                ["AppVersion"] = typeof(BugReportService).Assembly.GetName().Version?.ToString(),
                ["Culture"] = CultureInfo.CurrentCulture.Name,
                ["Is64BitProcess"] = Environment.Is64BitProcess,
                ["MachineName"] = Environment.MachineName
            };

            foreach (var kvp in CollectVisualInfo())
            {
                info[kvp.Key] = kvp.Value;
            }

            if (vsInfo != null)
            {
                foreach (var kvp in vsInfo)
                {
                    info[kvp.Key] = kvp.Value;
                }
            }

            return info;
        }

        // Screen/DPI info helps triage rendering and layout bug reports (e.g. WebView2
        // scaling issues that only show up at a particular DPI or multi-monitor setup).
        private static Dictionary<string, object> CollectVisualInfo()
        {
            try
            {
                var screens = Screen.AllScreens
                    .Select(screen => (object)new Dictionary<string, object>
                    {
                        ["DeviceName"] = screen.DeviceName,
                        ["Bounds"] = $"{screen.Bounds.Width}x{screen.Bounds.Height}",
                        ["WorkingArea"] = $"{screen.WorkingArea.Width}x{screen.WorkingArea.Height}",
                        ["BitsPerPixel"] = screen.BitsPerPixel,
                        ["Primary"] = screen.Primary
                    })
                    .ToList();

                var dpi = "unknown";

                try
                {
                    using (var graphics = Graphics.FromHwnd(IntPtr.Zero))
                    {
                        dpi = $"{graphics.DpiX}x{graphics.DpiY}";
                    }
                }
                catch
                {
                    // Not fatal: DPI just stays "unknown" if there's no desktop device context available.
                }

                return new Dictionary<string, object>
                {
                    ["ScreenCount"] = Screen.AllScreens.Length,
                    ["Screens"] = screens,
                    ["VirtualScreen"] = $"{SystemInformation.VirtualScreen.Width}x{SystemInformation.VirtualScreen.Height}",
                    ["PrimaryScreenDpi"] = dpi
                };
            }
            catch (Exception ex)
            {
                return new Dictionary<string, object>
                {
                    ["ScreenInfoError"] = ex.Message
                };
            }
        }

        private object CollectLastPrompt(string chatId)
        {
            if (string.IsNullOrWhiteSpace(chatId))
            {
                return null;
            }

            var chatPromptsPath = StoragePaths.GetChatPromptsPath(chatId);

            if (!workspaceFileService.DirectoryExists(chatPromptsPath))
            {
                return null;
            }

            var lastFile = workspaceFileService
                .EnumerateFiles(chatPromptsPath, "prompt_*.json", SearchOption.AllDirectories)
                .OrderByDescending(workspaceFileService.GetLastWriteTime)
                .FirstOrDefault();

            if (lastFile == null)
            {
                return null;
            }

            string content;

            try
            {
                content = workspaceFileService.Read(lastFile);
            }
            catch
            {
                return null;
            }

            object parsedContent;

            try
            {
                parsedContent = JToken.Parse(content);
            }
            catch
            {
                parsedContent = content;
            }

            return new
            {
                ChatMessageId = Path.GetFileName(Path.GetDirectoryName(lastFile)),
                FileName = Path.GetFileName(lastFile),
                Content = parsedContent
            };
        }
    }
}
