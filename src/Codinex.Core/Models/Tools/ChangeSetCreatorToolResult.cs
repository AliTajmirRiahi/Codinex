using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Codinex.Core.Models.Tools
{
    public sealed class WorkspaceChangeSuccess
    {
        /// <summary>
        /// Indicates whether the tool completed successfully.
        /// </summary>
        public string Status { get; set; } = "success";

        /// <summary>
        /// Indicates that the requested operation has been completed.
        /// </summary>
        public bool Completed { get; set; } = true;

        /// <summary>
        /// Indicates that the workspace was modified.
        /// </summary>
        public bool WorkspaceModified { get; set; } = true;

        /// <summary>
        /// Human-readable summary of the completed operation.
        /// </summary>
        public string Message { get; set; } =
            "All requested workspace changes have been successfully applied.";

        /// <summary>
        /// List of affected files.
        /// </summary>
        public List<ChangedFileResult> Files { get; set; } = [];
    }

    public sealed class ChangedFileResult
    {
        /// <summary>
        /// Workspace-relative file path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// CreateFile, EditFile, DeleteFile, RenameFile, ...
        /// </summary>
        public string Operation { get; set; }

        /// <summary>
        /// success / failed
        /// </summary>
        public string Status { get; set; } = "success";

        /// <summary>
        /// For EditFile / CreateFile: a snippet of the file AFTER the change was applied,
        /// showing the modified region(s) with a few lines of surrounding context. Lets the
        /// model verify what actually landed without a follow-up read_file / read_element.
        /// Null for operations where it does not apply (rename, move, delete).
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string AppliedRegion { get; set; }
    }
}
