using System;
using Microsoft.Extensions.DependencyInjection;

namespace Codify.Core.DependencyInjection.Models;

public sealed class RegistrationItem(
    Type service,
    Type implementation, 
    string module,
    RegistrationOrder registrationOrder,
    ServiceLifetime lifetime)
{
    public Type Service { get; } = service;

    public Type Implementation { get; } = implementation;

    public ServiceLifetime Lifetime { get; } = lifetime;

    public RegistrationOrder RegistrationOrder { get; set; } = registrationOrder;

    public string Module { get; } = module;
}