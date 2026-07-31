using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codify.Core.Models.Tools
{
    public class ChangeSetCreatorToolResult
    {
        public string Status { get; set; } = "Completed";

        public string Rule { get; set; } = "The tool results above are authoritative.\n\nrequested changes were successfully applied, do not call change_set_creator again.\n\nInstead, produce a final response to the user summarizing the completed work";

    }
}
