using System;
using System.Linq;
using System.Reflection;
using Codify.Core.Interfaces;
using Codify.Core.Tools;
using Codify.Core.Workspace.Prompt;
using Codify.VisualStudio.Tools;
using Microsoft.Extensions.DependencyInjection;

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
        RegisterImplementations<IAiTool>(
            services,
            assembly);

        services.AddSingleton<IAiToolRegistry, AiToolRegistry>();

        return services;
    }

    /// <summary>
    /// Registers all workspace services from the specified assembly.
    /// </summary>
    public static IServiceCollection AddWorkspaceServices(
        this IServiceCollection services,
        Assembly assembly)
    {
        foreach (var serviceType in WorkspaceServiceTypes)
        {
            RegisterImplementations(
                services,
                assembly,
                serviceType);
        }

        return services;
    }

    /// <summary>
    /// Gets all workspace service contracts that should be registered automatically.
    /// </summary>
    private static readonly Type[] WorkspaceServiceTypes =
    [
        typeof(IWorkspaceContextOrchestrator),

        typeof(IDiagnosticsProvider),
        typeof(IDiagnosticsFormatter),

        typeof(IBuildContextProvider),
        typeof(IBuildContextFormatter),

        //typeof(IGitProvider),
        //typeof(IGitFormatter),

        typeof(IOpenDocumentsProvider),
        typeof(IOpenDocumentsFormatter),

        //typeof(IProjectProvider),
        //typeof(IProjectFormatter)

    ];

    /// <summary>
    /// Registers all implementations of the specified service.
    /// </summary>
    private static void RegisterImplementations<TService>(
        IServiceCollection services,
        Assembly assembly)
    {
        RegisterImplementations(
            services,
            assembly,
            typeof(TService));
    }

    /// <summary>
    /// Registers all implementations of the specified service.
    /// </summary>
    private static void RegisterImplementations(
        IServiceCollection services,
        Assembly assembly,
        Type serviceType)
    {
        var implementationTypes = assembly
            .GetTypes()
            .Where(type =>
                !type.IsAbstract &&
                !type.IsInterface &&
                serviceType.IsAssignableFrom(type));

        foreach (var implementationType in implementationTypes)
        {
            services.AddSingleton(
                serviceType,
                implementationType);
        }
    }
}