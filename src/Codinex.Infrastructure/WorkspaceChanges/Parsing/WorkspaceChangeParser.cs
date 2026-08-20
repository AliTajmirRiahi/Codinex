using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Interfaces.WorkspaceChanges;
using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Infrastructure.WorkspaceChanges.Mapping;
using Codinex.Infrastructure.WorkspaceChanges.Parsing.Dtos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Codinex.Infrastructure.WorkspaceChanges.Parsing;

/// <summary>
/// Parses an AI response into a workspace change set.
/// </summary>
[AutoDiRegister(Modules.Workspace, RegistrationOrder.Features)]
public sealed class WorkspaceChangeParser(
    IJsonSerializer jsonSerializer,
    IWorkspaceChangeMapper mapper)
    : IWorkspaceChangeParser
{
    public Task<WorkspaceChangeParseResult> ParseAsync(
        JObject response,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (response == null)
        {
            return Task.FromResult(
                WorkspaceChangeParseResult.Failed(ParseError("Response cannot be null or empty.")));
        }

        try
        {
            NormalizeChanges(response);

            var dto = response.ToObject<WorkspaceChangeSetDto>();

            if (dto is null)
            {
                return Task.FromResult(
                    WorkspaceChangeParseResult.Failed(
                        ParseError("Failed to deserialize workspace change set.")));
            }

            var changeSet = mapper.Map(dto);

            return Task.FromResult(WorkspaceChangeParseResult.Successful(changeSet));
        }
        catch (Exception ex) when (ex is JsonException or WorkspaceChangeParseException)
        {
            return Task.FromResult(
                WorkspaceChangeParseResult.Failed(
                    ParseError($"Failed to parse workspace change set: {ex.Message}")));
        }
    }

    private static WorkspaceValidationError ParseError(string message)
    {
        return new WorkspaceValidationError(
            Guid.Empty,
            WorkspaceChangeErrorCode.InvalidChange,
            WorkspaceValidationCategory.AiRecoverable,
            message);
    }

    private static void NormalizeChanges(JObject response)
    {
        var changesToken = response["changes"];

        if (changesToken == null || changesToken.Type != JTokenType.String)
        {
            return;
        }

        var changesJson = changesToken.Value<string>();

        if (string.IsNullOrWhiteSpace(changesJson))
        {
            return;
        }

        var changesArray = JsonConvert.DeserializeObject<JArray>(changesJson);

        if (changesArray == null)
        {
            return;
        }

        response["changes"] = changesArray;
    }
}
