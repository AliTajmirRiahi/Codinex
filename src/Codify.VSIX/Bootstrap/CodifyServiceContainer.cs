// Path: Codify\Infrastructure\DependencyInjection\ServiceContainer.cs

using Codify.Core.Chat;
using Codify.Core.Conversation;
using Codify.Core.Interfaces;
using Codify.Core.UseCases;
using Codify.Infrastructure.AI.Capabilities;
using Codify.Infrastructure.AI.Providers;
using Codify.Infrastructure.Chat;
using Codify.Infrastructure.Conversation;
using Codify.Infrastructure.ModelManagement;
using Codify.Infrastructure.ModelManagement.Retrievers;
using Codify.VisualStudio;
using Codify.VisualStudio.Events.Build;
using Codify.VisualStudio.Interfaces;
using Codify.VisualStudio.Internal;
using Codify.VisualStudio.Logging;
using Codify.VisualStudio.References;
using Codify.VisualStudio.WebView;
using Codify.VisualStudio.Workspace.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using System;
using System.IO.Abstractions;
using Codify.Core.DependencyInjection;

namespace Codify.VSIX.Bootstrap
{
    public static class CodifyServiceContainer
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

            services.AddHttpClient();

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

            services.AddSingleton<IResourceServer>(sp => new WebViewResourceServer(typeof(Codify.UI.ToolWindows.CodifyToolWindowControl).Assembly, "Codify.UI.ToolWindows.Resources"));
            services.AddSingleton<IVisualStudioServices>(sp => new VisualStudioServices(package, sp.GetRequiredService<IUiThreadDispatcher>()));
            services.AddSingleton<IVsOutputLogger>(sp => new VsOutputLogger(pane));

            var report = ServiceRegistrar.Register(services, typeof(CodifyServiceContainer).Assembly);

            var text = RegistrationReportFormatter.Format(report);

            System.Diagnostics.Debug.WriteLine(text);

            Instance = services.BuildServiceProvider();
        }

        public static T Get<T>() where T : notnull
        {
            return Instance == null ? throw new InvalidOperationException("Codify service container has not been initialized.") : Instance.GetRequiredService<T>();
        }
    }
}
