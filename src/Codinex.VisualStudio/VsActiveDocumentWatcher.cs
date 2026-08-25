using EnvDTE;
using EnvDTE80;
using System;
using System.IO;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Documents;
using Codinex.Core.Interfaces.Services;
using Codinex.Core.Models.Documents;
using Codinex.VisualStudio.Interfaces;
using Codinex.VisualStudio.Internal;

namespace Codinex.VisualStudio;
#pragma warning disable VSTHRD010

[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Infrastructure)]
public sealed class VsActiveDocumentWatcher(IVisualStudioServices visualStudioServices, IUiThreadDispatcher uiThreadDispatcher) : VsServiceBase(visualStudioServices), IActiveDocumentWatcher, IStartupTask
{
    private WindowEvents _windowEvents;

    public event EventHandler<ActiveDocumentChangedEventArgs> ActiveDocumentChanged;

    public async Task StartAsync()
    {
        await uiThreadDispatcher.SwitchToMainThreadAsync();

        var events2 = (Events2)(await GetDteAsync()).Events;
        _windowEvents = events2.WindowEvents;
        _windowEvents.WindowActivated += OnWindowActivated;

    }
    private void OnWindowActivated(Window gotFocus, Window lostFocus)
    {
        _ = OnWindowActivatedAsync(gotFocus, lostFocus);
    }

    private async Task OnWindowActivatedAsync(Window gotFocus, Window lostFocus)
    {
        await uiThreadDispatcher.SwitchToMainThreadAsync();

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
#pragma warning restore VSTHRD010

