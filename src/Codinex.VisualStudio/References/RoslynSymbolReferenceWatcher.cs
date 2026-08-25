using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.References;
using Codinex.Core.Interfaces.Services;
using Codinex.Core.Models;
using Codinex.VisualStudio.Interfaces;
using Codinex.VisualStudio.Internal;
using Codinex.VisualStudio.References.Providers.Base;

namespace Codinex.VisualStudio.References
{
    /// <summary>
    /// Watches the live Roslyn workspace (not the file system) for document edits, so
    /// symbol-level reference providers (method/class/field/interface) can push add/remove/update
    /// events for individual symbols instead of the whole file being re-scanned.
    ///
    /// This reacts to <see cref="Workspace.WorkspaceChanged"/>, which fires from the live text
    /// buffer as you type — not just on save — and hands us the changed <see cref="Document"/>
    /// directly. That's what makes symbol-level diffing possible: a file-system watcher only
    /// knows "this file changed," but re-extracting just the one document lets us compare the
    /// new set of methods/classes/fields/interfaces against the previous set and report exactly
    /// what was added, removed, or edited.
    /// </summary>
    [AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Infrastructure)]
    public sealed class RoslynSymbolReferenceWatcher(
        IVisualStudioServices visualStudio,
        IUiThreadDispatcher uiThreadDispatcher,
        IEnumerable<IReferenceProvider> providers,
        IErrorHandler errorHandler)
        : VsServiceBase(visualStudio), ISymbolReferenceWatcher, IStartupTask
    {
        private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(800);

        private readonly IReadOnlyList<RoslynReferenceProviderBase> _providers = providers.OfType<RoslynReferenceProviderBase>().ToList();

        // Last known items per (provider, document), keyed by stable ReferenceItem.Id, so a new
        // extraction pass can be diffed against what the UI was last told about.
        private readonly ConcurrentDictionary<(RoslynReferenceProviderBase Provider, DocumentId DocumentId), Dictionary<string, ReferenceItem>> _cache = new();

        // Pending debounce timers per document, so a burst of keystrokes only triggers one diff pass.
        private readonly ConcurrentDictionary<DocumentId, CancellationTokenSource> _pendingChanges = new();

        private Microsoft.CodeAnalysis.Workspace _workspace;

        public event EventHandler<ReferenceItemChangedEventArgs> ReferenceAdded;
        public event EventHandler<ReferenceRemovedEventArgs> ReferenceRemoved;
        public event EventHandler<ReferenceItemChangedEventArgs> ReferenceUpdated;

        public async Task StartAsync()
        {
            await uiThreadDispatcher.SwitchToMainThreadAsync();

            _workspace = await GetWorkspaceAsync();

            if (_workspace == null)
                return;

            _workspace.WorkspaceChanged += OnWorkspaceChanged;
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            switch (e.Kind)
            {
                case WorkspaceChangeKind.DocumentAdded:
                case WorkspaceChangeKind.DocumentChanged:
                case WorkspaceChangeKind.DocumentReloaded:
                    ScheduleDocumentDiff(e.NewSolution, e.DocumentId);
                    return;

                case WorkspaceChangeKind.DocumentRemoved:
                    RemoveDocument(e.DocumentId);
                    return;
            }
        }

        private void ScheduleDocumentDiff(Solution solution, DocumentId documentId)
        {
            if (documentId == null || _providers.Count == 0)
                return;

            var cts = new CancellationTokenSource();

            _pendingChanges.AddOrUpdate(documentId, cts, (_, previous) =>
            {
                previous.Cancel();
                previous.Dispose();
                return cts;
            });

            _ = DebounceAndDiffAsync(solution, documentId, cts.Token);
        }

        private async Task DebounceAndDiffAsync(Solution solution, DocumentId documentId, CancellationToken token)
        {
            try
            {
                await Task.Delay(DebounceDelay, token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested)
                return;

            _pendingChanges.TryRemove(documentId, out _);

            var document = solution.GetDocument(documentId);

            if (document == null)
                return;

            try
            {
                await DiffDocumentAsync(document).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                errorHandler.Handle(ex, nameof(DiffDocumentAsync));
            }
        }

        /// <summary>
        /// Re-extracts references for the given document from every registered Roslyn provider
        /// and raises add/remove/update events for whatever changed since the last pass.
        /// Separated from the debounce/subscription plumbing so it can be exercised directly
        /// against an in-memory workspace (e.g. <c>AdhocWorkspace</c>) in tests.
        /// </summary>
        public async Task DiffDocumentAsync(Document document)
        {
            foreach (var provider in _providers)
            {
                var newItems = await provider.GetReferencesForDocumentAsync(document).ConfigureAwait(false);
                var newById = newItems.ToDictionary(i => i.Id);

                var cacheKey = (provider, document.Id);
                var oldById = _cache.TryGetValue(cacheKey, out var existing)
                    ? existing
                    : new Dictionary<string, ReferenceItem>();

                foreach (var item in newItems)
                {
                    if (!oldById.TryGetValue(item.Id, out var oldItem))
                    {
                        ReferenceAdded?.Invoke(this, new ReferenceItemChangedEventArgs(item));
                    }
                    else if (!ContentEquals(oldItem, item))
                    {
                        ReferenceUpdated?.Invoke(this, new ReferenceItemChangedEventArgs(item));
                    }
                }

                foreach (var oldItem in oldById.Values)
                {
                    if (!newById.ContainsKey(oldItem.Id))
                    {
                        ReferenceRemoved?.Invoke(this, new ReferenceRemovedEventArgs(oldItem.Id));
                    }
                }

                _cache[cacheKey] = newById;
            }
        }

        private void RemoveDocument(DocumentId documentId)
        {
            if (documentId == null)
                return;

            if (_pendingChanges.TryRemove(documentId, out var pending))
                pending.Cancel();

            foreach (var provider in _providers)
            {
                if (!_cache.TryRemove((provider, documentId), out var oldById))
                    continue;

                foreach (var oldItem in oldById.Values)
                {
                    ReferenceRemoved?.Invoke(this, new ReferenceRemovedEventArgs(oldItem.Id));
                }
            }
        }

        private static bool ContentEquals(ReferenceItem a, ReferenceItem b)
        {
            return a.Metadata?.Content == b.Metadata?.Content
                && a.Metadata?.Signature == b.Metadata?.Signature
                && a.Metadata?.StartLine == b.Metadata?.StartLine
                && a.Metadata?.EndLine == b.Metadata?.EndLine;
        }
    }
}
