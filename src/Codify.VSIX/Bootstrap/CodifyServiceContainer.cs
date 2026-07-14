// Path: Codify\Infrastructure\DependencyInjection\ServiceContainer.cs

using Codify.Core.Chat;
using Codify.Core.Conversation;
using Codify.Core.Interfaces;
using Codify.Core.Tools;
using Codify.Core.UseCases;
using Codify.Infrastructure.AI.Providers;
using Codify.Infrastructure.Chat;
using Codify.Infrastructure.Conversation;
using Codify.Infrastructure.IO;
using Codify.Infrastructure.Serialization;
using Codify.Infrastructure.Tools;
using Codify.Infrastructure.VisualStudio;
using Codify.Infrastructure.WebView;
using Codify.Storage;
using Codify.VisualStudio;
using Codify.VisualStudio.Diagnostics.Errors;
using Codify.VisualStudio.Hosting.Startup;
using Codify.VisualStudio.Interfaces;
using Codify.VisualStudio.Internal;
using Codify.VisualStudio.Logging;
using Codify.VisualStudio.References;
using Codify.VisualStudio.References.Providers;
using Codify.VisualStudio.Services;
using Codify.VisualStudio.Theme;
using Codify.VisualStudio.WebView;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using System;

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

            // Core Services (Singletons)
            services.AddSingleton<IResourceServer>(sp => new WebViewResourceServer(
                typeof(Codify.UI.ToolWindows.CodifyToolWindowControl).Assembly,
                "Codify.UI.ToolWindows.Resources"));
            services.AddSingleton<IVisualStudioServices>(sp => new VisualStudioServices(package));
            //services.AddSingleton<IIntentClassifier, IntentClassifier>();
            services.AddSingleton<IJsonSerializer, JsonSerializationService>();
            services.AddSingleton<IStorageService, FileStorageService>();
            services.AddSingleton<IPayloadBinder, NewtonsoftPayloadBinder>();
            services.AddSingleton<IUiThreadDispatcher, VsThreadDispatcher>();
            services.AddSingleton<IThemeService, VsThemeService>();
            services.AddSingleton<IWorkspaceContext, VsWorkspaceContext>();
            services.AddSingleton<IStorageService, FileStorageService>();
            services.AddSingleton<IExecutionPipeline, ExecutionPipeline>();
            services.AddSingleton<IVsOutputWindowService, VsOutputWindowService>();

            services.AddSingleton<IVsOutputLogger>(sp => new VsOutputLogger(pane));
            services.AddSingleton<IUserNotificationService>(
                _ => new VsUserNotificationService(package));
            services.AddSingleton<IErrorHandler, ErrorHandler>();


            // Storage & Configuration Managers
            services.AddSingleton<SettingsManager>();
            services.AddSingleton<ProviderManager>();
            services.AddSingleton<ChatManager>();
            services.AddSingleton<FileReferenceProvider>();
            services.AddSingleton<IReferenceProvider>(sp => sp.GetRequiredService<FileReferenceProvider>());
            services.AddSingleton<IActiveDocumentProvider>(sp => sp.GetRequiredService<FileReferenceProvider>());
            services.AddSingleton<IReferenceProvider, SystemReferenceProvider>();
            services.AddSingleton<IReferenceProvider, SolutionReferenceProvider>();
            services.AddSingleton<IReferenceProvider, MethodReferenceProvider>();
            services.AddSingleton<IReferenceProvider, ClassReferenceProvider>();
            services.AddSingleton<IReferenceProvider, InterfaceReferenceProvider>();
            services.AddSingleton<IReferenceProvider, FieldReferenceProvider>();
            services.AddSingleton<IReferenceProvider, FolderReferenceProvider>();

            // Reference Context Services
            services.AddSingleton<IReferenceContextFormatter, ReferenceContextFormatter>();
            services.AddSingleton<IChatMessageBuilder, ChatMessageBuilder>();

            // Register Manager
            services.AddSingleton(sp => new VsActiveDocumentWatcher(package));
            services.AddSingleton<IActiveDocumentWatcher>(sp => sp.GetRequiredService<VsActiveDocumentWatcher>());
            services.AddSingleton<IStartupTask>(sp => sp.GetRequiredService<VsActiveDocumentWatcher>());
            services.AddSingleton<ReferenceManager>();

            // Chat Logic
            services.AddSingleton<ChatSessionService>();
            services.AddSingleton<ChatUseCaseFactory>();

            // AI Providers (The Plugin System)
            // Register all available providers here
            services.AddSingleton<IAiProvider, OpenAiProvider>();
            services.AddSingleton<IAiProvider, GapGptProvider>();

            // Note: To add a local AI (e.g. Ollama), just create the class 
            // and add: services.AddSingleton<IAiProvider, OllamaProvider>();

            // Use Cases (Business Logic)
            services.AddTransient<IConversationEngine, ConversationEngine>();
            services.AddTransient<ISendChatMessageUseCase, SendChatMessageUseCase>();

            // WebView Infrastructure
            services.AddSingleton<IWebViewClient, WebViewClient>();
            services.AddSingleton<IWebViewMessageRouter, WebViewMessageRouter>();

            services.AddSingleton<IAiTool, PingTool>();

            services.AddSingleton<IAiToolRegistry, AiToolRegistry>();

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
