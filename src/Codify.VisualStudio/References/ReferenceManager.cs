using Codify.Core.Interfaces;
using Codify.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Codify.VisualStudio.Hosting.Startup;

namespace Codify.VisualStudio.References
{
    public class ActiveDocumentUpdatedEventArgs(ReferenceItem activeDocument) : EventArgs
    {
        public ReferenceItem ActiveDocument { get; } = activeDocument;
    }
    public class ReferenceManager
    {
        private readonly IActiveDocumentWatcher _activeDocumentWatcher;
        private readonly IActiveDocumentProvider _activeDocumentProvider;
        private readonly IExecutionPipeline _pipeline;
        private readonly IErrorHandler _errorHandler;
        private readonly IReadOnlyList<IReferenceProvider> _providers;

        private ReferenceItem _activeDocumentItem;

        public event EventHandler<ActiveDocumentUpdatedEventArgs> ActiveDocumentUpdated;

        public ReferenceManager(
            IEnumerable<IReferenceProvider> providers,
            IActiveDocumentWatcher activeDocumentWatcher,
            IActiveDocumentProvider activeDocumentProvider,
            IExecutionPipeline pipeline,
            IErrorHandler errorHandler)
        {
            _activeDocumentWatcher = activeDocumentWatcher;
            _activeDocumentProvider = activeDocumentProvider;
            _pipeline = pipeline;
            _errorHandler = errorHandler;
            _providers = providers.ToList();

            _activeDocumentWatcher.ActiveDocumentChanged += OnActiveDocumentChanged;
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
    }
}
