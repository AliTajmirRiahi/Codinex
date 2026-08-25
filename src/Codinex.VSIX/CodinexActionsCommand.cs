using System;
using System.ComponentModel.Design;
using System.IO;
using System.Threading.Tasks;
using Codinex.Core.Interfaces.Services;
using Codinex.Core.Models;
using Codinex.VisualStudio.Interfaces;
using Codinex.VSIX.Bootstrap;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Codinex.VSIX
{
    /// <summary>
    /// Handles the "Codinex Actions" editor context menu submenu
    /// ("Add to Chat" / "Describe" / "Document" / "Make it Better").
    /// The submenu is only visible while the Codinex tool window is already open.
    /// </summary>
    public sealed class CodinexActionsCommand
    {
        public const int AddToChatCommandId = 0x0112;
        public const int DescribeSelectionCommandId = 0x0113;
        public const int DocumentSelectionCommandId = 0x0114;
        public const int MakeItBetterSelectionCommandId = 0x0115;

        public static readonly Guid CommandSet = new Guid("f695020e-3b35-40ae-b466-57fc5bbe2d6c");

        private readonly AsyncPackage package;

        private CodinexActionsCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));

            if (commandService == null)
            {
                throw new ArgumentNullException(nameof(commandService));
            }

            AddCommand(commandService, AddToChatCommandId, OnAddToChat);
            AddCommand(commandService, DescribeSelectionCommandId, (s, e) => OnRunCommandOnSelection("/describe"));
            AddCommand(commandService, DocumentSelectionCommandId, (s, e) => OnRunCommandOnSelection("/document"));
            AddCommand(commandService, MakeItBetterSelectionCommandId, (s, e) => OnRunCommandOnSelection("/makeItBetter"));
        }

        public static CodinexActionsCommand Instance { get; private set; }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new CodinexActionsCommand(package, commandService);
        }

        private void AddCommand(OleMenuCommandService commandService, int commandId, EventHandler invokeHandler)
        {
            var command = new OleMenuCommand(invokeHandler, new CommandID(CommandSet, commandId));
            command.BeforeQueryStatus += OnBeforeQueryStatus;
            commandService.AddCommand(command);
        }

        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var command = (OleMenuCommand)sender;
            var isVisible = IsToolWindowVisible();

            command.Visible = isVisible;
            command.Enabled = isVisible;
        }

        private bool IsToolWindowVisible()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var window = this.package.FindToolWindow(typeof(CodinexToolWindow), 0, false);

            if (window?.Frame is not IVsWindowFrame frame)
            {
                return false;
            }

            return frame.IsVisible() == Microsoft.VisualStudio.VSConstants.S_OK;
        }

        private void OnAddToChat(object sender, EventArgs e)
        {
            var pipeline = CodinexServiceContainer.Get<IExecutionPipeline>();

            _ = pipeline.RunAsync(async () =>
            {
                var selection = await BuildSelectionReferenceAsync();

                if (selection == null)
                {
                    return;
                }

                var router = CodinexServiceContainer.Get<IWebViewMessageRouter>();

                await router.SendSelectedCodeReferenceAsync(selection);
            }, nameof(OnAddToChat));
        }

        private void OnRunCommandOnSelection(string commandName)
        {
            var pipeline = CodinexServiceContainer.Get<IExecutionPipeline>();

            _ = pipeline.RunAsync(async () =>
            {
                var selection = await BuildSelectionReferenceAsync();

                if (selection == null)
                {
                    return;
                }

                var router = CodinexServiceContainer.Get<IWebViewMessageRouter>();

                await router.RunCommandOnSelectionAsync(selection, commandName);
            }, nameof(OnRunCommandOnSelection));
        }

        private async Task<ReferenceItem> BuildSelectionReferenceAsync()
        {
            var visualStudio = CodinexServiceContainer.Get<IVisualStudioServices>();

            var dte = await visualStudio.GetDteAsync();

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var activeDocument = dte?.ActiveDocument;

            if (activeDocument?.Selection is not TextSelection selection ||
                string.IsNullOrWhiteSpace(selection.Text))
            {
                return null;
            }

            var filePath = activeDocument.FullName;
            var fileName = Path.GetFileName(filePath);

            return new ReferenceItem
            {
                Id = $"selection:{Guid.NewGuid()}",
                Name = $"Selected Code {fileName} ({selection.TopLine}-{selection.BottomLine})",
                Description = $"{fileName} ({selection.TopLine}-{selection.BottomLine})",
                Type = ReferenceKind.SelectedCode,
                Value = filePath,
                Icon = "symbols/symbol-code",
                Color = "--vscode-charts-purple",
                Metadata = new ReferenceMetadata
                {
                    FilePath = filePath,
                    ContainerName = Path.GetDirectoryName(filePath),
                    ProjectName = activeDocument.ProjectItem?.ContainingProject?.Name ?? string.Empty,
                    Content = selection.Text,
                    StartLine = selection.TopLine,
                    EndLine = selection.BottomLine
                }
            };
        }
    }
}
