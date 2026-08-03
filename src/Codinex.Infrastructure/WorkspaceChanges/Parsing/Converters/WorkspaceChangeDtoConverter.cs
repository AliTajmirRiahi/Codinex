using System;
using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Models.WorkspaceChanges;
using Codify.Infrastructure.WorkspaceChanges.Parsing.Dtos;
using Codify.Infrastructure.WorkspaceChanges.Parsing.Factories;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Codify.Infrastructure.WorkspaceChanges.Parsing.Converters;

/// <summary>
/// Deserializes workspace change DTOs based on the 'kind' discriminator.
/// </summary>
[AutoDiRegister(Modules.JSON, RegistrationOrder.Features)]
public sealed class WorkspaceChangeDtoConverter : JsonConverter<WorkspaceChangeDto>
{
    public override WorkspaceChangeDto ReadJson(
        JsonReader reader,
        Type objectType,
        WorkspaceChangeDto? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        var json = JObject.Load(reader);

        var kindToken = json["kind"];

        if (kindToken is null)
        {
            throw new JsonSerializationException(
                "Required property 'kind' was not found.");
        }

        if (!Enum.TryParse(
                kindToken.Value<string>(),
                ignoreCase: true,
                out WorkspaceChangeKind kind))
        {
            throw new JsonSerializationException(
                $"Unknown workspace change kind '{kindToken}'.");
        }

        var dto = WorkspaceChangeDtoFactory.Create(kind);

        serializer.Populate(json.CreateReader(), dto);

        return dto;
    }

    public override void WriteJson(
        JsonWriter writer,
        WorkspaceChangeDto? value,
        JsonSerializer serializer)
    {
        throw new NotSupportedException(
            "Workspace change DTO serialization is not supported.");
    }

    public override bool CanWrite => false;
}