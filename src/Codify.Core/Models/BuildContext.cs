using System.Collections.Generic;

namespace Codify.Core.Models
{
    /// <summary>
    /// Represents the latest build result information.
    /// </summary>
    public sealed class BuildContext
    {
        public IList<BuildMessageItem> Messages { get; set; }
            = new List<BuildMessageItem>();
    }
}