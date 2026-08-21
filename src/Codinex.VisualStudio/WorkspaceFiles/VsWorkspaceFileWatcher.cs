using EnvDTE;
using EnvDTE80;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
using Codinex.VisualStudio.Interfaces;
using Codinex.VisualStudio.Internal;

namespace Codinex.VisualStudio.WorkspaceFiles;
#pragma warning disable VSTHRD010

/// <summary>
/// Watches solution/project item and document-save events so the UI can react to files being
/// added, removed, or edited without needing to re-scan the whole solution.
/// Event sinks (<see cref="ProjectItemsEvents"/>, <see cref="DocumentEvents"/>) are held as fields
/// to keep the underlying COM event connections alive for the lifetime of this service.
/// </summary>
[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Infrastructure)]
public sealed class VsWorkspaceFileWatcher(
    IVisualStudioServices visualStudioServices,
    IUiThreadDispatcher uiThreadDispatcher,
    IWorkspaceFileService workspaceFileService)
    : VsServiceBase(visualStudioServices), IWorkspaceFileWatcher, IStartupTask
{
    // GUID for a physical file ProjectItem in VS.
    private const string PhysicalFileKind = "{6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C}";

    private ProjectItemsEvents _projectItemsEvents;
    private DocumentEvents _documentEvents;

    public event EventHandler<WorkspaceFileChangedEventArgs> FileAdded;
    public event EventHandler<WorkspaceFileChangedEventArgs> FileRemoved;
    public event EventHandler<WorkspaceFileChangedEventArgs> FileChanged;

    public async Task StartAsync()
    {
        await uiThreadDispatcher.SwitchToMainThreadAsync();

        var events2 = (Events2)(await GetDteAsync()).Events;

        _projectItemsEvents = events2.ProjectItemsEvents;
        _projectItemsEvents.ItemAdded += OnItemAdded;
        _projectItemsEvents.ItemRemoved += OnItemRemoved;
        _projectItemsEvents.ItemRenamed += OnItemRenamed;

        _documentEvents = events2.DocumentEvents;
        _documentEvents.DocumentSaved += OnDocumentSaved;
    }

    private void OnItemAdded(ProjectItem item)
    {
        _ = OnItemAddedAsync(item);
    }

    private async Task OnItemAddedAsync(ProjectItem item)
    {
        await uiThreadDispatcher.SwitchToMainThreadAsync();

        var filePath = TryGetFilePath(item);

        if (string.IsNullOrEmpty(filePath) || !workspaceFileService.Exists(filePath))
            return;

        FileAdded?.Invoke(this, new WorkspaceFileChangedEventArgs
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath)
        });
    }

    private void OnItemRemoved(ProjectItem item)
    {
        _ = OnItemRemovedAsync(item);
    }

    private async Task OnItemRemovedAsync(ProjectItem item)
    {
        await uiThreadDispatcher.SwitchToMainThreadAsync();

        var filePath = TryGetFilePath(item);

        if (string.IsNullOrEmpty(filePath))
            return;

        FileRemoved?.Invoke(this, new WorkspaceFileChangedEventArgs
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath)
        });
    }

    private void OnItemRenamed(ProjectItem item, string oldName)
    {
        _ = OnItemRenamedAsync(item, oldName);
    }

    private async Task OnItemRenamedAsync(ProjectItem item, string oldName)
    {
        await uiThreadDispatcher.SwitchToMainThreadAsync();

        var filePath = TryGetFilePath(item);

        if (!string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(oldName))
        {
            var directory = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(directory))
            {
                FileRemoved?.Invoke(this, new WorkspaceFileChangedEventArgs
                {
                    FilePath = Path.Combine(directory, oldName),
                    FileName = oldName
                });
            }
        }

        if (!string.IsNullOrEmpty(filePath) && workspaceFileService.Exists(filePath))
        {
            FileAdded?.Invoke(this, new WorkspaceFileChangedEventArgs
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath)
            });
        }
    }

    private void OnDocumentSaved(Document document)
    {
        _ = OnDocumentSavedAsync(document);
    }

    private async Task OnDocumentSavedAsync(Document document)
    {
        await uiThreadDispatcher.SwitchToMainThreadAsync();

        var filePath = document?.FullName;

        if (string.IsNullOrEmpty(filePath) || !workspaceFileService.Exists(filePath))
            return;

        FileChanged?.Invoke(this, new WorkspaceFileChangedEventArgs
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath)
        });
    }

    private static string TryGetFilePath(ProjectItem item)
    {
        try
        {
            if (item?.Kind == PhysicalFileKind && item.FileCount > 0)
                return item.FileNames[1];
        }
        catch (COMException)
        {
            // Item may already be detached from its project by the time we inspect it
            // (e.g. rapid delete/undo) — treat as "no path available".
        }

        return null;
    }
}
#pragma warning restore VSTHRD010
