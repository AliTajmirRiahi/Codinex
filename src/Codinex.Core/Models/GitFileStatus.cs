namespace Codify.Core.Models
{
    /// <summary>
    /// Represents the Git status of a file.
    /// </summary>
    public enum GitFileStatus
    {
        Modified = 0,
        Added = 1,
        Deleted = 2,
        Renamed = 3,
        Copied = 4,
        Untracked = 5
    }
}