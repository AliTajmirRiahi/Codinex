using Codify.Core.Abstractions;
using Codify.Core.UseCases;
using Codify.Infrastructure;
using Codify.Infrastructure.AiProviders;
using Codify.Infrastructure.ChatSessions;
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
using Codify.Infrastructure.DependencyInjection;

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
        private readonly IPayloadBinder _payloadBinder;

        // These will be initialized once WebView is ready
        private IWebViewMessageRouter _messageRouter;
        private IWebViewClient _webViewClient;

        public CodifyToolWindowControl()
        {
            // Initialize services that don't depend on WebView
            _themeService = CodifyServiceContainer.Get<IThemeService>();
            _resourceServer = CodifyServiceContainer.Get<IResourceServer>(); 
            _payloadBinder = CodifyServiceContainer.Get<IPayloadBinder>();

            InitializeComponent();

            Unloaded += OnUnloaded;
            SetupThemeIntegration();

            _ = InitializeWebViewAsync();
        }

        private async Task InitializeWebViewAsync()
        {
            try
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

                // 5. Subscribe to incoming messages from JS
                WebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                WebView.CoreWebView2.Navigate(
                    "http://codify.resources/Chat/view/chat-view.html"
                );
            }
            catch (Exception ex)
            {
                // Log the exception for debugging.
                System.Diagnostics.Debug.WriteLine($"WebView2 Error: {ex.Message}");
                MessageBox.Show($"WebView2 Init Failed:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetupThemeIntegration()
        {
            //1.Initial Apply
            WebView.NavigationCompleted += OnNavigationCompleted;

            // 2. Subscribe to changes
            _themeService.ThemeChanged += OnThemeChanged;
        }

        private async void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var router = CodifyServiceContainer.Get<IWebViewMessageRouter>();
                // The router now has everything it needs to be injected via DI
                await router.HandleMessageAsync(e.WebMessageAsJson);
            }
            catch (Exception ex)
            {
                // Log the exception for debugging.
                System.Diagnostics.Debug.WriteLine($"WebView2 Error: {ex.Message}");
                MessageBox.Show($"WebView2 Init Failed:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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