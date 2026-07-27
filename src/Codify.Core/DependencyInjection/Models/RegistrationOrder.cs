namespace Codify.Core.DependencyInjection.Models;

/// <summary>
/// Defines the registration order for dependency injection.
/// Services with lower values are registered first.
/// </summary>
public enum RegistrationOrder
{
    /// <summary>
    /// Fundamental framework services.
    /// </summary>
    Foundation = 0,

    /// <summary>
    /// Infrastructure services.
    /// </summary>
    Infrastructure = 100,

    /// <summary>
    /// Platform integration services (Visual Studio, Roslyn, etc.).
    /// </summary>
    Platform = 200,

    /// <summary>
    /// Workspace related services.
    /// </summary>
    Workspace = 300,

    /// <summary>
    /// AI related services.
    /// </summary>
    AI = 400,

    /// <summary>
    /// High level application features.
    /// </summary>
    Features = 500,

    /// <summary>
    /// Tools and commands.
    /// </summary>
    Tools = 600,
}