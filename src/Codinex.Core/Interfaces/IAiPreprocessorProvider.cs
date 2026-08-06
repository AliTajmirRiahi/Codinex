using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models;

namespace Codinex.Core.Interfaces
{
    public interface IAiPreprocessorProvider : IAiProvider
    {
        /// <summary>
        /// Executes this provider as the Preprocessor AI and returns the structured preprocessing decision.
        /// </summary>
        Task<AiPreprocessorResult> PreprocessAsync(
            IReadOnlyList<ChatMessage> messages,
            CancellationToken cancellationToken = default);
    }
}