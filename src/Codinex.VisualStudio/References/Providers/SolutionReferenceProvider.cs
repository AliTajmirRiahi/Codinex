// Infrastructure/References/Providers/SolutionReferenceProvider.cs

using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.References;
using Codinex.Core.Interfaces.Workspace;
using Codinex.Core.Models;
using Codinex.VisualStudio.Interfaces;
using Codinex.VisualStudio.Internal;

namespace Codinex.VisualStudio.References.Providers
{
    [AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Platform)]
    public class SolutionReferenceProvider(IVisualStudioServices visualStudio, IWorkspaceContext workspaceContext)
        : VsServiceBase(visualStudio), IReferenceProvider
    {
        private readonly IVisualStudioServices _visualStudio = visualStudio;
        private readonly IWorkspaceContext _workspaceContext = workspaceContext;

        public async Task<IReadOnlyList<ReferenceItem>> GetReferencesAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var items = new List<ReferenceItem>();
            var dte = await _visualStudio.GetDteAsync();

            if (dte?.Solution is not { IsOpen: true })
            {
                return items;
            }

            // Add the Solution itself as a reference
            items.Add(new ReferenceItem
            {
                Id = "sol:root",
                Name = "Solution",
                Description = _workspaceContext.SolutionName,
                Type = ReferenceKind.Solution,
                Icon = "fileTypes/file_type_sln2",
                Value = dte.Solution.FullName
            });

            // Add all Projects in the Solution
            foreach (Project project in dte.Solution.Projects)
            {
                if (project == null) continue;

                // Skip Solution Folders (Virtual Folders) if you only want actual projects
                // The GUID {66A26720-8FB5-11D2-AA7E-00C04F688DDE} is for Solution Folders
                if (project.Kind == "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}") continue;

                try
                {
                    items.Add(new ReferenceItem
                    {
                        Id = $"proj:{project.UniqueName}",
                        Name = project.Name,
                        Description = "Project",
                        Type = ReferenceKind.Project,
                        Icon = "fileTypes/icon-project",
                        Value = project.FullName
                    });
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    // Some project types (e.g. unsupported/unloaded/CPS projects still initializing)
                    // throw from DTE property accessors like FullName. Skip them rather than letting
                    // one broken project take down the whole reference list.
                }
                catch (NotImplementedException)
                {
                }
            }

            return items;
        }
    }
}
