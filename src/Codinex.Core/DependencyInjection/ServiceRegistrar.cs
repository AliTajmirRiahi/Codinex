using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Codinex.Core.DependencyInjection;

public static class ServiceRegistrar
{
    public static RegistrationReport Register(
        IServiceCollection services,
        Assembly root)
    {
        var report = new RegistrationReport();

        var assemblies = LoadCodinexAssemblies(root);

        var registrations = assemblies
            .SelectMany(GetRegistrations)
            .OrderBy(x => x.Attribute.RegistrationOrder)
            .ToList();

        foreach (var registration in registrations)
        {
            RegisterService(
                services,
                report,
                registration.Type,
                registration.Attribute);
        }

        return report;
    }

    /// <summary>
    /// Loads the root assembly and all referenced to Codify assemblies recursively.
    /// </summary>
    private static IReadOnlyCollection<Assembly> LoadCodinexAssemblies(
        Assembly rootAssembly)
    {
        var assemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

        LoadAssemblyRecursive(rootAssembly);

        return assemblies.Values;

        void LoadAssemblyRecursive(Assembly assembly)
        {
            var name = assembly.GetName().Name;

            if (assemblies.ContainsKey(name))
                return;

            assemblies.Add(name, assembly);

            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                if (!reference.Name.StartsWith("Codinex.", StringComparison.Ordinal))
                    continue;

                try
                {
                    var referencedAssembly = Assembly.Load(reference);

                    LoadAssemblyRecursive(referencedAssembly);
                }
                catch
                {
                    // Ignore assemblies that cannot be loaded.
                }
            }
        }
    }

    private static IEnumerable<(Type Type, AutoDiRegisterAttribute Attribute)> GetRegistrations(
        Assembly assembly)
    {
        return assembly
            .GetTypes()
            .Where(t =>
                t.IsClass &&
                !t.IsAbstract &&
                !t.IsGenericTypeDefinition)
            .Select(t => new
            {
                Type = t,
                Attribute = t.GetCustomAttribute<AutoDiRegisterAttribute>()
            })
            .Where(x => x.Attribute != null)
            .Select(x => (x.Type, x.Attribute!));
    }

    /// <summary>
    /// Registers a service using the most appropriate strategy.
    /// </summary>
    private static void RegisterService(
        IServiceCollection services,
        RegistrationReport report,
        Type implementation,
        AutoDiRegisterAttribute attribute)
    {
        var interfaces = implementation
            .GetInterfaces()
            .Where(i => i != typeof(IDisposable))
            .ToArray();

        switch (interfaces.Length)
        {
            //
            // No interface -> register self
            //
            case 0:
                RegisterSelf(
                    services,
                    report,
                    implementation,
                    attribute);

                return;
            //
            // Single interface -> direct registration
            //
            case 1:
                services.Add(new ServiceDescriptor(
                    interfaces[0],
                    implementation,
                    attribute.Lifetime));

                report.Items.Add(new RegistrationItem(
                    interfaces[0],
                    implementation,
                    attribute.Module,
                    attribute.RegistrationOrder,
                    attribute.Lifetime));

                return;
        }

        //
        // Multiple interfaces -> single implementation instance
        //
        RegisterSelf(
            services,
            report,
            implementation,
            attribute);

        foreach (var serviceType in interfaces)
        {
            services.Add(new ServiceDescriptor(
                serviceType,
                sp => sp.GetRequiredService(implementation),
                attribute.Lifetime));

            report.Items.Add(new RegistrationItem(
                serviceType,
                implementation,
                attribute.Module,
                attribute.RegistrationOrder,
                attribute.Lifetime));
        }
    }

    private static void RegisterSelf(
        IServiceCollection services,
        RegistrationReport report,
        Type implementation,
        AutoDiRegisterAttribute attribute)
    {
        services.Add(new ServiceDescriptor(
            implementation,
            implementation,
            attribute.Lifetime));

        report.Items.Add(new RegistrationItem(
            implementation,
            implementation,
            attribute.Module,
            attribute.RegistrationOrder,
            attribute.Lifetime));
    }
}