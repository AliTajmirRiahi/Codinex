using System.Linq;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Context;
using Codinex.Core.Models;
using Codinex.Storage.Interfaces;

namespace Codinex.VisualStudio.Workspace.Providers;

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