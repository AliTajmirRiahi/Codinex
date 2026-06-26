using Codify.Core.Abstractions;
using Codify.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codify.Infrastructure.References.Providers
{
    public class SystemReferenceProvider : IReferenceProvider
    {
        public Task<IReadOnlyList<ReferenceItem>> GetReferencesAsync()
        {
            var items = new List<ReferenceItem>
            {
                // Git Context
                new ReferenceItem { Id = "git-changes", Name = "Git Changes", Description = "Pending changes in workspace", Type = ReferenceKind.Git, Icon = "fileTypes/file_type_git", Value = "git:changes" },
                new ReferenceItem { Id = "git-staged", Name = "Git Staged", Description = "Staged files for commit", Type = ReferenceKind.Git, Icon = "fileTypes/file_type_git", Value = "git:staged" },

                // Output/Log Context
                new ReferenceItem { Id = "out-build", Name = "Build Output", Description = "Latest build logs", Type = ReferenceKind.Output, Icon = "fileTypes/icon-build", Value = "out:build" },
                new ReferenceItem { Id = "out-tests", Name = "Test Results", Description = "Recent test execution logs", Type = ReferenceKind.Log, Icon = "fileTypes/file_type_light_testcafe", Value = "out:tests" },
                new ReferenceItem { Id = "out-debug", Name = "Debug Console", Description = "Active debug session logs", Type = ReferenceKind.Log, Icon = "fileTypes/debug-console", Value = "out:debug" },
                
                //// Solution/Project Context
                //new ReferenceItem { Id = "sol-root", Name = "Solution Overview", Description = "Active solution structure", Type = ReferenceKind.Solution, Icon = "icon-solution", Value = "sol:root" },
                //new ReferenceItem { Id = "sol-deps", Name = "Dependencies", Description = "Project references and NuGet packages", Type = ReferenceKind.Project, Icon = "icon-package", Value = "sol:deps" },
                
                // Terminal/External
                new ReferenceItem { Id = "sys-term", Name = "Terminal History", Description = "Recent terminal command outputs", Type = ReferenceKind.Log, Icon = "fileTypes/terminal-compact", Value = "sys:terminal" }
            };

            return Task.FromResult<IReadOnlyList<ReferenceItem>>(items);
        }
    }
}
