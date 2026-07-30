using Codify.Core.Models.WorkspaceChanges;
using Codify.Infrastructure.WorkspaceChanges.Parsing.Dtos;

namespace Codify.Infrastructure.WorkspaceChanges.Mapping
{
    public interface IWorkspaceChangeMapper
    {
        WorkspaceChangeSet Map(WorkspaceChangeSetDto dto);
    }
}
