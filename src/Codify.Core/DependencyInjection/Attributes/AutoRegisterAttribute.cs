using System;
using Codify.Core.DependencyInjection.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Codify.Core.DependencyInjection.Attributes;


/// <summary>
/// Marks a service for automatic dependency injection registration.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AutoDiRegisterAttribute(
    string module,
    RegistrationOrder registrationOrder,
    ServiceLifetimeWrapper lifetime = ServiceLifetimeWrapper.Singleton
    )
    : Attribute
{
    public readonly ServiceLifetimeWrapper _lifetime = lifetime;
    /// <summary>
    /// Gets the registration lifetime.
    /// </summary>
    public ServiceLifetime Lifetime {
        get
        {
            return _lifetime switch
            {
                ServiceLifetimeWrapper.Singleton => ServiceLifetime.Singleton,
                ServiceLifetimeWrapper.Scoped => ServiceLifetime.Scoped,
                ServiceLifetimeWrapper.Transient => ServiceLifetime.Transient,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    /// <summary>
    /// Gets the module used for diagnostics and reporting.
    /// </summary>
    public string Module { get; } = module;

    /// <summary>
    /// Gets the registration order.
    /// </summary>
    public RegistrationOrder RegistrationOrder { get; } = registrationOrder;



}