using System;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Interfaces;
using Codify.Core.Interfaces.WorkspaceChanges;
using Codify.Core.Models.WorkspaceChanges;
using Codify.Infrastructure.WorkspaceChanges.Mapping;
using Codify.Infrastructure.WorkspaceChanges.Parsing.Dtos;

namespace Codify.Infrastructure.WorkspaceChanges.Parsing;

/// <summary>
/// Parses an AI response into a workspace change set.
/// </summary>
[AutoDiRegister(Modules.Workspace, RegistrationOrder.Features)]
public sealed class WorkspaceChangeParser(
    IJsonSerializer jsonSerializer,
    IWorkspaceChangeMapper mapper)
    : IWorkspaceChangeParser
{
    public Task<WorkspaceChangeSet> ParseAsync(
        string response,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(response))
        {
            throw new ArgumentException(
                "Response cannot be null or empty.",
                nameof(response));
        }

        var dto = jsonSerializer.Deserialize<WorkspaceChangeSetDto>(response);

        if (dto is null)
        {
            throw new InvalidOperationException(
                "Failed to deserialize workspace change set.");
        }

        var changeSet = mapper.Map(dto);

        return Task.FromResult(changeSet);
    }
}