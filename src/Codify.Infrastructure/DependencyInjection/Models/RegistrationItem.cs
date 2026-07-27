using Codify.Infrastructure.DependencyInjection.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Codify.Infrastructure.DependencyInjection.Models;

public sealed class RegistrationItem
{
    public Type Service { get; } 

    public Type Implementation { get; }

    public ServiceLifetime Lifetime { get; }

    public ModuleType Module { get; }
}