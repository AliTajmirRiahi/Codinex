using Codify.Core.Abstractions;
using Codify.Core.Models;
using Codify.Infrastructure.Execution;
using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace Codify.Infrastructure.References
{
    public class ActiveDocumentUpdatedEventArgs : EventArgs
    {
        public ActiveDocumentUpdatedEventArgs(ReferenceItem activeDocument)
        {
            ActiveDocument = activeDocument;
        }

        public ReferenceItem ActiveDocument { get; }
    }
    public class ReferenceManager
    {
        private readonly IActiveDocumentWatcher _activeDocumentWatcher;
        private readonly IActiveDocumentProvider _activeDocumentProvider;
        private readonly ExecutionPipeline _pipeline;
        private readonly IReadOnlyList<IReferenceProvider> _providers;

        private ReferenceItem _activeDocumentItem;

        public event EventHandler<ActiveDocumentUpdatedEventArgs> ActiveDocumentUpdated;

        public ReferenceManager(IEnumerable<IReferenceProvider> providers, IActiveDocumentWatcher activeDocumentWatcher, IActiveDocumentProvider activeDocumentProvider, ExecutionPipeline pipeline)
        {
            _activeDocumentWatcher = activeDocumentWatcher;
            _activeDocumentProvider = activeDocumentProvider;
            _pipeline = pipeline;
            _providers = providers.ToList();

            _activeDocumentWatcher.ActiveDocumentChanged += OnActiveDocumentChanged;
        }

        public async Task<IReadOnlyList<ReferenceItem>> GetAllReferencesAsync()
        {
            var tasks = _providers.Select(provider =>
            {
                try
                {
                    return provider.GetReferencesAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
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
