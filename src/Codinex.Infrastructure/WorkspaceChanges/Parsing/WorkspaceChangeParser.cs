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
    public Task<WorkspaceChangeSet> ParseAsync(
        JObject response,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (response == null)
        {
            throw new ArgumentException(
                "Response cannot be null or empty.",
                nameof(response));
        }

        NormalizeChanges(response);

        var changesToken = response["changes"];

        System.Diagnostics.Debug.WriteLine(
            $"Changes token type: {changesToken?.Type}");

        System.Diagnostics.Debug.WriteLine(
            $"Changes value: {changesToken}");

        var tt = Newtonsoft.Json.JsonConvert.SerializeObject(response);

        var dto = response.ToObject<WorkspaceChangeSetDto>();

        if (dto is null)
        {
            throw new InvalidOperationException(
                "Failed to deserialize workspace change set.");
        }

        var changeSet = mapper.Map(dto);

        return Task.FromResult(changeSet);
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