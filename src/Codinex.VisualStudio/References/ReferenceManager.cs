using Codinex.VisualStudio.Hosting.Startup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;

namespace Codinex.VisualStudio.References
{
    public class ActiveDocumentUpdatedEventArgs(ReferenceItem activeDocument) : EventArgs
    {
        public ReferenceItem ActiveDocument { get; } = activeDocument;
    }

    public class ReferenceItemEventArgs(ReferenceItem item) : EventArgs
    {
        public ReferenceItem Item { get; } = item;
    }

    public class ReferenceRemovedEventArgs(string filePath) : EventArgs
    {
        public string FilePath { get; } = filePath;
    }

    [AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Infrastructure)]
    public class ReferenceManager
    {
        private readonly IActiveDocumentWatcher _activeDocumentWatcher;
        private readonly IActiveDocumentProvider _activeDocumentProvider;
        private readonly IWorkspaceFileWatcher _workspaceFileWatcher;
        private readonly IFileReferenceBuilder _fileReferenceBuilder;
        private readonly IExecutionPipeline _pipeline;
        private readonly IErrorHandler _errorHandler;
        private readonly IReadOnlyList<IReferenceProvider> _providers;

        private ReferenceItem _activeDocumentItem;

        public event EventHandler<ActiveDocumentUpdatedEventArgs> ActiveDocumentUpdated;
        public event EventHandler<ReferenceItemEventArgs> ReferenceAdded;
        public event EventHandler<ReferenceRemovedEventArgs> ReferenceRemoved;
        public event EventHandler<ReferenceItemEventArgs> ReferenceUpdated;

        public ReferenceManager(
            IEnumerable<IReferenceProvider> providers,
            IActiveDocumentWatcher activeDocumentWatcher,
            IActiveDocumentProvider activeDocumentProvider,
            IWorkspaceFileWatcher workspaceFileWatcher,
            IFileReferenceBuilder fileReferenceBuilder,
            IExecutionPipeline pipeline,
            IErrorHandler errorHandler)
        {
            _activeDocumentWatcher = activeDocumentWatcher;
            _activeDocumentProvider = activeDocumentProvider;
            _workspaceFileWatcher = workspaceFileWatcher;
            _fileReferenceBuilder = fileReferenceBuilder;
            _pipeline = pipeline;
            _errorHandler = errorHandler;
            _providers = providers.ToList();

            _activeDocumentWatcher.ActiveDocumentChanged += OnActiveDocumentChanged;

            _workspaceFileWatcher.FileAdded += OnFileAdded;
            _workspaceFileWatcher.FileRemoved += OnFileRemoved;
            _workspaceFileWatcher.FileChanged += OnFileChanged;
        }

        public async Task<IReadOnlyList<ReferenceItem>> GetAllReferencesAsync()
        {
            var tasks = _providers.Select(async provider =>
            {
                try
                {
                    return await provider.GetReferencesAsync();
                }
                catch (Exception ex)
                {
                    _errorHandler.Handle(
                        ex,
                        nameof(GetAllReferencesAsync));

                    throw;
                }
            });

            var results = await Task.WhenAll(tasks);

            return results
                .Where(items => items != null)
                .SelectMany(items => items)
                .ToList();
        }

        public async Task<ReferenceItem> GetActiveDocumentAsync()
        {
            _activeDocumentItem ??= await _activeDocumentProvider.GetActiveDocumentAsync();

            return _activeDocumentItem;
        }

        private void OnActiveDocumentChanged(object sender, ActiveDocumentChangedEventArgs e)
        {
            _ = _pipeline.RunAsync(
                () => HandleActiveDocumentChangedAsync(e),
                nameof(HandleActiveDocumentChangedAsync));
        }

        private async Task HandleActiveDocumentChangedAsync(ActiveDocumentChangedEventArgs e)
        {
            _activeDocumentItem = await _activeDocumentProvider.GetActiveDocumentAsync(e.FilePath);

            ActiveDocumentUpdated?.Invoke(this, new ActiveDocumentUpdatedEventArgs(_activeDocumentItem));
        }

        private void OnFileAdded(object sender, WorkspaceFileChangedEventArgs e)
        {
            _ = _pipeline.RunAsync(
                () => HandleFileAddedAsync(e.FilePath),
                nameof(HandleFileAddedAsync));
        }

        private async Task HandleFileAddedAsync(string filePath)
        {
            var item = await _fileReferenceBuilder.BuildFileReferenceAsync(filePath);

            if (item == null)
                return;

            ReferenceAdded?.Invoke(this, new ReferenceItemEventArgs(item));
        }

        private void OnFileRemoved(object sender, WorkspaceFileChangedEventArgs e)
        {
            ReferenceRemoved?.Invoke(this, new ReferenceRemovedEventArgs(e.FilePath));
        }

        private void OnFileChanged(object sender, WorkspaceFileChangedEventArgs e)
        {
            _ = _pipeline.RunAsync(
                () => HandleFileChangedAsync(e.FilePath),
                nameof(HandleFileChangedAsync));
        }

        private async Task HandleFileChangedAsync(string filePath)
        {
            var item = await _fileReferenceBuilder.BuildFileReferenceAsync(filePath);

            if (item == null)
                return;

            ReferenceUpdated?.Invoke(this, new ReferenceItemEventArgs(item));
        }
    }
}
