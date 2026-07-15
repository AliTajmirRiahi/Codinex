using Codify.Core.Interfaces;
using Codify.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Infrastructure.ModelManagement
{
    public class ResourceModelLoader : IModelResourceLoader
    {
        public async Task<List<AiModel>> LoadAsync(
            AiProvider provider,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken);

            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation)!;

            var resourcePath = Path.Combine(
                assemblyDirectory,
                "Resources",
                $"{provider.Id}_models.json");

            if (!File.Exists(resourcePath))
            {
                throw new FileNotFoundException(
                    $"Resource file not found: {provider.Id}_models.json",
                    resourcePath);
            }

            var json = File.ReadAllText(resourcePath);

            var models = JsonConvert.DeserializeObject<List<AiModel>>(json);

            if (models == null)
            {
                throw new InvalidOperationException(
                    $"Failed to deserialize models from {provider.Id}_models.json");
            }

            return models;
        }

    }
}