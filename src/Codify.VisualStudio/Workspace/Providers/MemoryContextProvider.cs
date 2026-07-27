using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.Storage.Interfaces;
using System.Linq;

namespace Codify.VisualStudio.Workspace.Providers;

/// <summary>
/// Provides long-term workspace memory.
/// </summary>
[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Platform)]
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