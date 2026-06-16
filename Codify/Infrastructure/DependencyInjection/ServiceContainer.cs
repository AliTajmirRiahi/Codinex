// Path: Codify\Infrastructure\DependencyInjection\ServiceContainer.cs

using Codify.Core.Abstractions;
using Codify.Core.UseCases;
using Codify.Infrastructure.AiProviders;
using Codify.Infrastructure.ChatSessions;
using Codify.Infrastructure.Factory;
using Codify.Infrastructure.Serialization;
using Codify.Infrastructure.Theme;
using Codify.Infrastructure.WebView;
using Codify.Storage;
using Microsoft.Extensions.DependencyInjection;
using System;
using Newtonsoft.Json;

namespace Codify.Infrastructure.DependencyInjection
{
    public static class CodifyServiceContainer
    {
        public static IServiceProvider Instance { get; private set; }

        public static void Initialize()
        {
            var services = new ServiceCollection();
            services.AddSingleton<JsonSerializer>(sp =>
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
            services.AddSingleton<IJsonSerializer, JsonSerializationService>();
            services.AddSingleton<IStorageService, FileStorageService>();
            services.AddSingleton<IPayloadBinder, NewtonsoftPayloadBinder>();
            services.AddSingleton<IThemeService, VsThemeService>();
            services.AddSingleton<IStorageService, FileStorageService>();

            // 2. Storage & Configuration Managers
            services.AddSingleton<SettingsManager>();
            services.AddSingleton<ProviderManager>();
            services.AddSingleton<ChatManager>();

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
            => Instance.GetRequiredService<T>();
    }
}
