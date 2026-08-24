using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Bugsnag;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
using Codinex.Storage.Services;
using Newtonsoft.Json.Linq;

namespace Codinex.Infrastructure.BugReporting
{
    [AutoDiRegister(Modules.AI, RegistrationOrder.Infrastructure)]
    public sealed class BugReportService(IWorkspaceFileService workspaceFileService) : IBugReportService
    {
        // BugSnag payloads have a practical ~1MB ceiling; keep the output log well under
        // that so metadata never gets rejected outright.
        private const int MaxOutputLogLength = 800_000;

        private readonly Lazy<Client> _client = new(() =>
            new Client(new Configuration(BugsnagOptions.ApiKey)));

        public Task<BugReportResult> SubmitAsync(
            string chatId,
            string description,
            string outputLog,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return Task.FromResult(BugReportResult.Failed("A description of the bug is required."));
            }

            try
            {
                var exception = new BugReportedException(description);

                var systemInfo = CollectSystemInfo();
                var lastPrompt = CollectLastPrompt(chatId);
                var truncatedOutputLog = Truncate(outputLog);

                _client.Value.Notify(exception, report =>
                {
                    report.Event.Context = description;
                    report.Event.Metadata.Add("Report", new Dictionary<string, object>
                    {
                        ["ChatId"] = chatId,
                        ["Description"] = description
                    });
                    report.Event.Metadata.Add("System", systemInfo);
                    report.Event.Metadata.Add("OutputLog", new Dictionary<string, object>
                    {
                        ["Log"] = truncatedOutputLog
                    });
                    report.Event.Metadata.Add("LastPrompt", lastPrompt);
                });

                return Task.FromResult(BugReportResult.Ok("Bug report sent. Thank you!"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(BugReportResult.Failed($"Failed to send bug report: {ex.Message}"));
            }
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

        private static Dictionary<string, object> CollectSystemInfo()
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
