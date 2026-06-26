// Infrastructure/References/Providers/SolutionReferenceProvider.cs
using Codify.Core.Abstractions;
using Codify.Core.Models;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Codify.Infrastructure.References.Providers
{
    public class SolutionReferenceProvider : IReferenceProvider
    {
        private readonly IServiceProvider _serviceProvider;

        public SolutionReferenceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<IReadOnlyList<ReferenceItem>> GetReferencesAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var items = new List<ReferenceItem>();
            var dte = _serviceProvider.GetService(typeof(DTE)) as DTE2;

            if (dte?.Solution == null || !dte.Solution.IsOpen)
            {
                return items;
            }

            // Add the Solution itself as a reference
            items.Add(new ReferenceItem
            {
                Id = "sol:root",
                Name = dte.Solution.FullName.Split('\\', '/')[1], // Solution Name
                Description = "Entire Solution",
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

            return items;
        }
    }
}
