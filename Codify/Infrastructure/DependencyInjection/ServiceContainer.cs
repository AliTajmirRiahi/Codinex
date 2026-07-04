// Path: Codify\Infrastructure\DependencyInjection\ServiceContainer.cs

using Codify.Core.Abstractions;
using Codify.Core.UseCases;
using Codify.Infrastructure.AiProviders;
using Codify.Infrastructure.ChatSessions;
using Codify.Infrastructure.Errors;
using Codify.Infrastructure.Execution;
using Codify.Infrastructure.Factory;
using Codify.Infrastructure.Filters;
using Codify.Infrastructure.Logging;
using Codify.Infrastructure.References;
using Codify.Infrastructure.References.Providers;
using Codify.Infrastructure.Serialization;
using Codify.Infrastructure.Theme;
using Codify.Infrastructure.VisualStudio;
using Codify.Infrastructure.WebView;
using Codify.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using System;

namespace Codify.Infrastructure.DependencyInjection
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

            // 1. Core Services (Singletons)
            services.AddSingleton<IResourceServer>(sp => new WebViewResourceServer(
                typeof(Codify.UI.ToolWindows.CodifyToolWindowControl).Assembly,
                "Codify.UI.ToolWindows.Resources"));
            services.AddSingleton<IIntentClassifier, IntentClassifier>();
            services.AddSingleton<IJsonSerializer, JsonSerializationService>();
            services.AddSingleton<IStorageService, FileStorageService>();
            services.AddSingleton<IPayloadBinder, NewtonsoftPayloadBinder>();
            services.AddSingleton<IThemeService, VsThemeService>();
            services.AddSingleton<IStorageService, FileStorageService>();
            services.AddSingleton<ExecutionPipeline>();
            // Pseudo-registration example.
            // Adjust according to your actual service container implementation.
            services.AddSingleton<IVsOutputLogger>(sp => new VsOutputLogger(pane));
            services.AddSingleton<IUserNotificationService>(
                _ => new VsUserNotificationService(package));
            services.AddSingleton<IErrorHandler, ErrorHandler>();

            // 2. Storage & Configuration Managers
            services.AddSingleton<SettingsManager>();
            services.AddSingleton<ProviderManager>();
            services.AddSingleton<ChatManager>();
            services.AddSingleton(sp => new FileReferenceProvider(package));
            services.AddSingleton<IReferenceProvider>(sp => sp.GetRequiredService<FileReferenceProvider>());
            services.AddSingleton<IReferenceProvider, SystemReferenceProvider>();
            services.AddSingleton<IReferenceProvider>(sp => new SolutionReferenceProvider(package));

            services.AddSingleton<IActiveDocumentProvider>(sp => sp.GetRequiredService<FileReferenceProvider>());


            // Register Manager
            services.AddSingleton(sp=> new VsActiveDocumentWatcher(package));
            services.AddSingleton<IActiveDocumentWatcher>(sp => sp.GetRequiredService<VsActiveDocumentWatcher>());
            services.AddSingleton<IStartupTask>(sp => sp.GetRequiredService<VsActiveDocumentWatcher>());
            services.AddSingleton<ReferenceManager>();

            // Chat Logic
            services.AddSingleton<ChatSessionService>();
            services.AddSingleton<ChatUseCaseFactory>();

            // 3. AI Providers (The Plugin System)
            // Register all available providers here
            services.AddSingleton<IAiProvider, OpenAiProvider>();
            services.AddSingleton<IAiProvider, GapGptProvider>();

            // Note: To add a local AI (e.g. Ollama), just create the class 
            // and add: services.AddSingleton<IAiProvider, OllamaProvider>();

            // 4. Use Cases (Business Logic)
            services.AddTransient<ISendChatMessageUseCase, SendChatMessageUseCase>();

            // 5. WebView Infrastructure
            services.AddSingleton<IWebViewClient, WebViewClient>();
            services.AddSingleton<IWebViewMessageRouter, WebViewMessageRouter>();

            Instance = services.BuildServiceProvider();
        }

        public static T Get<T>() where T : notnull
        {
            if (Instance == null)
                throw new InvalidOperationException("Codify service container has not been initialized.");

            return Instance.GetRequiredService<T>();
        }
    }
}
