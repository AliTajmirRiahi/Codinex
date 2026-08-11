namespace Codinex.Core.Models.WorkspaceChanges;

/// <summary>
/// Well-known values for <see cref="TextFileChange.Operation"/>.
/// </summary>
public static class TextChangeOperations
{
    public const string Replace = "replace";

    public const string InsertBefore = "insert_before";

    public const string InsertAfter = "insert_after";

    public const string Delete = "delete";
}
