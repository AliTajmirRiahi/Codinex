using System.Linq;
using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.Core.Workspace.Prompt;
using Codify.Storage.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.VisualStudio.Workspace.Providers;

/// <summary>
/// Provides long-term workspace memory.
/// </summary>
public sealed class MemoryContextProvider(IMemoryManager memoryManager) : IMemoryContextProvider
{
    public MemoryContext GetContext()
    {
        return new MemoryContext
        {
            MemoryDocument = new MemoryDocument()
            {
                Facts = memoryManager.GetAll().ToList(),
            }
        };
    }
}