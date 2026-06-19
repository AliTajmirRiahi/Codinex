using Codify.Core.Abstractions;
using Codify.Core.UseCases;
using Codify.Infrastructure;
using Codify.Infrastructure.AiProviders;
using Codify.Infrastructure.ChatSessions;
using Codify.Infrastructure.DependencyInjection;
using Codify.Infrastructure.Execution;
using Codify.Infrastructure.Serialization;
using Codify.Infrastructure.Theme;
using Codify.Infrastructure.VisualStudio;
using Codify.Infrastructure.WebView;
using Microsoft.VisualStudio.Shell;
using Microsoft.Web.WebView2.Core;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Codify.Infrastructure.Errors;

namespace Codify.UI.ToolWindows
{
    /// <summary>
    /// Interaction logic for CodifyToolWindowControl.
    /// </summary>
    public partial class CodifyToolWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodifyToolWindowControl"/> class.
        /// </summary>
        private readonly IThemeService _themeService;
        private readonly IResourceServer _resourceServer;
        private readonly IUserNotificationService _userNotificationService;

        public CodifyToolWindowControl()
        {
            // Initialize services that don't depend on WebView
            _themeService = CodifyServiceContainer.Get<IThemeService>();
            _resourceServer = CodifyServiceContainer.Get<IResourceServer>();

            InitializeComponent();

            Unloaded += OnUnloaded;
            SetupThemeIntegration();

            _userNotificationService = CodifyServiceContainer.Get<IUserNotificationService>();

            var pipeline = CodifyServiceContainer.Get<ExecutionPipeline>();

            _ = pipeline.RunAsync(
                InitializeWebViewAsync,
                nameof(InitializeWebViewAsync),
                showMessageBox: true);
        }

        private async Task InitializeWebViewAsync()
        {
            // Define the user data folder path to avoid permission issues.
            var userDataFolder = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "CodifyExtension");

            // Create the WebView2 environment using the custom folder.
            var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

            // Initialize WebView2 with the environment.
            await WebView.EnsureCoreWebView2Async(environment);

            WebView.CoreWebView2.OpenDevToolsWindow();

            // Get services from our DI Container
            var webViewClient = CodifyServiceContainer.Get<IWebViewClient>();

            webViewClient.Initialize(WebView);

            // Set up the resource server mapping
            _resourceServer.Attach(WebView.CoreWebView2);

            await RegisterWebViewHandlersAsync();

            WebView.CoreWebView2.Navigate(
                "http://codify.resources/Chat/view/chat-view.html"
            );
        }

        private async Task RegisterWebViewHandlersAsync()
        {
            var errorHandler = CodifyServiceContainer.Get<IErrorHandler>();
            // 5. Subscribe to incoming messages from JS
            WebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

            // In your WebView initialization code:
            await WebView.EnsureCoreWebView2Async();

            // Catch navigation errors (404, DNS, etc.)
            WebView.CoreWebView2.NavigationCompleted += (s, e) => {
                if (!e.IsSuccess)
                {
                    errorHandler.HandleUiError(
                        "WebView_Navigation",
                        "Ui_Navigation_Error",
                        $"Navigation failed with error: {e.WebErrorStatus}",
                        $"URL: {WebView.Source}"
                    );

                   _userNotificationService.ShowError(ErrorMessages.StartupError);
                }
            };

            // Catch resource loading errors (Sub-resources like JS/CSS files)
            WebView.CoreWebView2.WebResourceResponseReceived += (s, e) => {
                if (e.Response.StatusCode >= 400)
                {
                    errorHandler.HandleUiError(
                        "WebView_Resource",
                        "Ui_ResourceResponse_Error",
                        $"Failed to load: {e.Request.Uri}",
                        $"Status Code: {e.Response.StatusCode}"
                    );

                    _userNotificationService.ShowError(ErrorMessages.StartupError);
                }
            };
        }

        private void SetupThemeIntegration()
        {
            //1.Initial Apply
            WebView.NavigationCompleted += OnNavigationCompleted;

            // 2. Subscribe to changes
            _themeService.ThemeChanged += OnThemeChanged;
        }

        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var router = CodifyServiceContainer.Get<IWebViewMessageRouter>();
            var pipeline = CodifyServiceContainer.Get<ExecutionPipeline>();

            _ = pipeline.RunAsync(
                () => router.HandleMessageAsync(e.WebMessageAsJson),
                "WebViewMessageRouter");
        }
        private void OnThemeChanged(object sender, EventArgs e)
        {
            // Must be on UI Thread
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ApplyThemeToWebViewAsync();
            });
        }
        private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            _ = ApplyThemeToWebViewAsync();
        }

        private async Task ApplyThemeToWebViewAsync()
        {
            if (WebView.CoreWebView2 == null) return;

            var cssVariables = _themeService.GetCurrentThemeAsCssVariables();

            await WebView.CoreWebView2.ExecuteScriptAsync(cssVariables);
        }
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (WebView.CoreWebView2 != null)
            {
                WebView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
                WebView.NavigationCompleted -= OnNavigationCompleted;
            }
            WebView.Dispose();
            _themeService.ThemeChanged -= OnThemeChanged;
        }
    }
}