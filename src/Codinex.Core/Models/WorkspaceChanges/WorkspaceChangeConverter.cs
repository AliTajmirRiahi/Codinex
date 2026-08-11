using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Codinex.Core.Models.WorkspaceChanges;

/// <summary>
/// Deserializes concrete <see cref="WorkspaceChange"/> subtypes based on the 'kind' discriminator,
/// so persisted/round-tripped change sets don't rely on Newtonsoft's assembly-qualified TypeNameHandling.
/// </summary>
public sealed class WorkspaceChangeConverter : JsonConverter<WorkspaceChange>
{
    public override WorkspaceChange ReadJson(
        JsonReader reader,
        Type objectType,
        WorkspaceChange existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        var json = JObject.Load(reader);

        var kindToken = json["kind"] ?? json["Kind"];

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

        WorkspaceChange change = kind switch
        {
            WorkspaceChangeKind.EditFile => new EditFileChange(),
            WorkspaceChangeKind.CreateFile => new CreateFileChange(),
            WorkspaceChangeKind.DeleteFile => new DeleteFileChange(),
            WorkspaceChangeKind.RenameFile => new RenameFileChange(),
            WorkspaceChangeKind.MoveFile => new MoveFileChange(),
            WorkspaceChangeKind.CreateDirectory => new CreateDirectoryChange(),
            WorkspaceChangeKind.DeleteDirectory => new DeleteDirectoryChange(),
            WorkspaceChangeKind.RenameDirectory => new RenameDirectoryChange(),
            WorkspaceChangeKind.MoveDirectory => new MoveDirectoryChange(),
            _ => throw new JsonSerializationException($"Unhandled workspace change kind '{kind}'.")
        };

        serializer.Populate(json.CreateReader(), change);

        return change;
    }

    public override void WriteJson(JsonWriter writer, WorkspaceChange value, JsonSerializer serializer)
    {
        throw new NotSupportedException(
            "Workspace change serialization is not supported.");
    }

    // [JsonConverter] on the abstract base is inherited by every concrete subtype, and
    // JsonConverter<T>.CanConvert matches subtypes too — so writing would otherwise
    // re-enter this same converter forever ("self referencing loop detected"). CanWrite
    // = false tells Newtonsoft to skip the converter for writes and fall back to normal
    // reflection-based serialization of the concrete type; only reads need the 'kind'
    // discriminator logic above.
    public override bool CanWrite => false;
}
