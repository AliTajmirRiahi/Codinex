using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Codinex.Storage.Models;
using Codinex.Core.Models;

namespace Codinex.Storage.Interfaces
{
    public interface IMemoryManager
    {
        Task InitializeAsync();

        IReadOnlyList<MemoryFact> GetAll();

        MemoryFact Get(Guid id);

        Task<MemoryFact> RememberAsync(string title, string content);

        Task<MemoryFact> UpdateAsync(Guid id, string title, string content);

        Task<MemoryFact> ForgetAsync(Guid id);

        Task<bool> SaveAsync();
    }
}