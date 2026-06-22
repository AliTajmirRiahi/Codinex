using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codify.Storage.Models;

namespace Codify.Core.Models
{
    public class ChatMessage
    {
        public string Content { get; set; }
        public string Role { get; set; } // "user", "assistant", "system"
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Indicates whether this request should use streaming output.
        /// Default is false to preserve current behavior.
        /// </summary>
        public bool Stream { get; set; } = true;

    }
}
