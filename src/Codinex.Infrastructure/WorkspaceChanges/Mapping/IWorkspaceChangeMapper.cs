using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Infrastructure.WorkspaceChanges.Parsing.Dtos;

namespace Codinex.Infrastructure.WorkspaceChanges.Mapping
{
    public interface IWorkspaceChangeMapper
    {
        WorkspaceChangeSet Map(WorkspaceChangeSetDto dto);
    }
}
