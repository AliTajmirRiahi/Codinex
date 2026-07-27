using System;
using Microsoft.Extensions.DependencyInjection;

namespace Codify.Infrastructure.DependencyInjection.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class AutoRegisterAttribute(
    ModuleType module,
    ServiceLifetime lifetime = ServiceLifetime.Singleton)
    : Attribute
{
    public ServiceLifetime Lifetime { get; } = lifetime;

    public ModuleType Module { get; } = module;
}