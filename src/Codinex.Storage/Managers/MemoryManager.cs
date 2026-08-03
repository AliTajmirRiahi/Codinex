using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.Storage.Interfaces;
using Codify.Storage.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Codify.Storage.Managers
{
    [AutoDiRegister(Modules.Storage, RegistrationOrder.Foundation)]
    public sealed class MemoryManager(
        IStorageService storage,
        IWorkspaceContext workspaceContext) : IMemoryManager
    {
        private readonly MemoryDocument _memory = new();

        public async Task InitializeAsync()
        {
            var path = StoragePaths.GetWorkspaceMemoryPath(WorkspaceName);

            if (await storage.ExistsAsync(path))
            {
                var document = await storage.LoadAsync<MemoryDocument>(path);

                if (document == null) return;
                _memory.Facts.Clear();
                _memory.Facts.AddRange(document.Facts);

                return;
            }

            await SaveAsync();
        }

        public IReadOnlyList<MemoryFact> GetAll()
        {
            return _memory.Facts.AsReadOnly();
        }

        public MemoryFact Get(Guid id)
        {
            return _memory.Facts.FirstOrDefault(f => f.Id == id)
                ?? throw new InvalidOperationException($"Memory fact '{id}' was not found.");
        }

        public async Task<MemoryFact> RememberAsync(string title, string content)
        {
            var fact = _memory.Facts.FirstOrDefault(f =>
                string.Equals(f.Title, title, StringComparison.OrdinalIgnoreCase));

            if (fact != null)
            {
                fact.Content = content;
                fact.UpdatedAt = DateTime.UtcNow;

                await SaveAsync();

                return fact;
            }

            fact = new MemoryFact
            {
                Id = Guid.NewGuid(),
                Title = title,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _memory.Facts.Add(fact);

            await SaveAsync();

            return fact;
        }

        public async Task<MemoryFact> UpdateAsync(Guid id, string title, string content)
        {
            var fact = Get(id);

            fact.Title = title;
            fact.Content = content;
            fact.UpdatedAt = DateTime.UtcNow;

            await SaveAsync();

            return fact;
        }

        public async Task<MemoryFact> ForgetAsync(Guid id)
        {
            var fact = Get(id);

            _memory.Facts.Remove(fact);

            await SaveAsync();

            return fact;
        }

        public async Task<bool> SaveAsync()
        {
            var path = StoragePaths.GetWorkspaceMemoryPath(WorkspaceName);

            await storage.SaveAsync(path, _memory);

            return true;
        }

        private string WorkspaceName =>
            workspaceContext.IsSolutionOpen
                ? workspaceContext.SolutionName
                : "DefaultSolution";
    }
}