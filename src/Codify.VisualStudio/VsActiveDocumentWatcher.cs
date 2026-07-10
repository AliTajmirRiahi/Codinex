using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.IO;
using Codify.Core.Interfaces;
using Codify.Core.Models;

namespace Codify.VisualStudio
{
    public sealed class VsActiveDocumentWatcher(AsyncPackage package) : IActiveDocumentWatcher, IStartupTask
    {
        private readonly DTE2 _dte = ThreadHelper.JoinableTaskFactory?.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            return (DTE2)await package.GetServiceAsync(typeof(DTE));
        });
        private WindowEvents _windowEvents;

        public event EventHandler<ActiveDocumentChangedEventArgs> ActiveDocumentChanged;

        public void Start()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var events2 = (Events2)_dte.Events;
            _windowEvents = events2.WindowEvents;
            _windowEvents.WindowActivated += OnWindowActivated;
        }

        private void OnWindowActivated(Window gotFocus, Window lostFocus)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var document = gotFocus.Document;
            if (document == null || string.IsNullOrWhiteSpace(document.FullName))
            {
                return;
            }

            ActiveDocumentChanged?.Invoke(this, new ActiveDocumentChangedEventArgs
            {
                FilePath = document.FullName,
                FileName = Path.GetFileName(document.FullName)
            });
        }
    }

}
