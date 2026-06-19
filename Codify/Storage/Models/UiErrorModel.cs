using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codify.Storage.Models
{
    public class UiErrorModel
    {
        /// <summary>
        /// Message type (should be "error")
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Source module where the error occurred (e.g. chatController, aiService)
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// JavaScript stack trace if available
        /// </summary>
        public string? Stack { get; set; }
    }
}
