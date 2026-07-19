using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Codify.Core.Tools;
using Codify.VisualStudio.Tools;

namespace Codify.VSIX.Bootstrap;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all AI tools from the specified assembly.
    /// </summary>
    public static IServiceCollection AddAiTools(
        this IServiceCollection services,
        Assembly assembly)
    {
        var toolTypes = assembly
            .GetTypes()
            .Where(t =>
                !t.IsAbstract &&
                !t.IsInterface &&
                typeof(IAiTool).IsAssignableFrom(t));

        foreach (var toolType in toolTypes)
        {
            services.AddSingleton(typeof(IAiTool), toolType);
        }

        services.AddSingleton<IAiToolRegistry, AiToolRegistry>();

        return services;
    }
}