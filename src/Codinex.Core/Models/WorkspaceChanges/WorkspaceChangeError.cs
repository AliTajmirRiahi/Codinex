using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codinex.Core.Models.WorkspaceChanges;

public enum WorkspaceChangeErrorCode
{
    // Validation
    InvalidChange,

    // Matching
    SearchNotFound,
    MultipleMatches,
    NoUniqueMatch,

    // File
    FileNotFound,
    FileAlreadyExists,

    // Directory
    DirectoryNotFound,
    DirectoryAlreadyExists,

    // IO
    AccessDenied,
    IoError,

    InvalidPath,
    AbsolutePathNotAllowed,
    PathOutsideWorkspace,

    // Unknown
    Unknown
}

public sealed class WorkspaceChangeError
{
    public WorkspaceChangeErrorCode Code { get; set; }

    public string FilePath { get; set; }

    public Guid ChangeId { get; set; }

    public string Message { get; set; }
}