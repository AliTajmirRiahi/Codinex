using Codify.Core.Abstractions;
using Codify.Infrastructure;
using Codify.Infrastructure.Theme;
using Codify.Infrastructure.WebView;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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
        private readonly IWebViewMessageRouter _messageRouter;
        private readonly IThemeService _themeService;
        private readonly IResourceServer _resourceServer;

        public CodifyToolWindowControl(
            IWebViewMessageRouter messageRouter,
            IThemeService themeService,
            IResourceServer resourceServer)
        {
            _messageRouter = messageRouter;
            _themeService = themeService;
            _resourceServer = resourceServer;

            InitializeComponent();

            Unloaded += OnUnloaded;

            SetupThemeIntegration();

            _ = InitializeWebViewAsync();
        }

        public CodifyToolWindowControl() : this(
            new WebViewMessageRouter(),
            new VsThemeService(),
            new WebViewResourceServer(
                typeof(CodifyToolWindowControl).Assembly,
                "Codify.UI.ToolWindows.Resources"))
        {
        }
        private async Task InitializeWebViewAsync()
        {
            try
            {
                // 1. Define the user data folder path to avoid permission issues.
                var userDataFolder = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CodifyExtension");

                // 2. Create the WebView2 environment using the custom folder.
                var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

                // 3. Initialize WebView2 with the environment.
                await WebView.EnsureCoreWebView2Async(environment);

                _resourceServer.Attach(WebView.CoreWebView2);

                WebView.CoreWebView2.Navigate(
                    "http://codify.resources/Chat/chat-view.html"
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

        private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var json = e.WebMessageAsJson;

            Task.Delay(800).ContinueWith(_ =>
            {
                var response ="{\"type\":\"AI_RESPONSE\",\"payload\":\"Message received ✅\"}";

                WebView.Dispatcher.Invoke(() =>
                {
                    WebView.CoreWebView2.PostWebMessageAsJson(response);
                });
            });
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
            WebView.NavigationCompleted -= OnNavigationCompleted;
            _themeService.ThemeChanged -= OnThemeChanged;
        }
    }
}