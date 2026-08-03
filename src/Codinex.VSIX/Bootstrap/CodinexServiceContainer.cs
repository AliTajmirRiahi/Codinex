// Path: Codinex\Infrastructure\DependencyInjection\ServiceContainer.cs

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using System;
using System.IO.Abstractions;
using Codinex.Core.DependencyInjection;
using Codinex.Core.Interfaces;
using Codinex.VisualStudio.Interfaces;
using Codinex.VisualStudio.Internal;
using Codinex.VisualStudio.Logging;
using Codinex.VisualStudio.WebView;
using Microsoft.Extensions.DependencyInjection;

namespace Codinex.VSIX.Bootstrap
{
    public static class CodinexServiceContainer
    {
        public static IServiceProvider Instance { get; private set; }

        /// <summary>
        /// Indicates whether the DI container has been initialized.
        /// This is useful during package bootstrap where errors may happen before DI is ready.
        /// </summary>
        public static bool IsInitialized => Instance != null;

        public static void Initialize(
            AsyncPackage package, IVsOutputWindowPane pane)
        {
            var services = new ServiceCollection();

            services.AddSingleton(sp =>
            {
                var serializer = new JsonSerializer
                {
                    // Optional: configure your global settings here
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                    Formatting = Newtonsoft.Json.Formatting.None
                };
                return serializer;
            });

            services.AddSingleton<IFileSystem, FileSystem>();

            services.AddSingleton<IResourceServer>(sp => new WebViewResourceServer(typeof(Codinex.UI.ToolWindows.CodinexToolWindowControl).Assembly, "Codinex.UI.ToolWindows.Resources"));
            services.AddSingleton<IVisualStudioServices>(sp => new VisualStudioServices(package, sp.GetRequiredService<IUiThreadDispatcher>()));
            services.AddSingleton<IVsOutputLogger>(sp => new VsOutputLogger(pane));

            var report = ServiceRegistrar.Register(services, typeof(CodinexServiceContainer).Assembly);

            var text = RegistrationReportFormatter.Format(report);

            System.Diagnostics.Debug.WriteLine(text);

            Instance = services.BuildServiceProvider();

        }

        public static T Get<T>() where T : notnull
        {
            return Instance == null ? throw new InvalidOperationException("Codinex service container has not been initialized.") : Instance.GetRequiredService<T>();
        }
    }
}
