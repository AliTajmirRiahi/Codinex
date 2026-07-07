using Codify.Core.Abstractions;
using Codify.Infrastructure.DependencyInjection;
using Codify.Infrastructure.Errors;
using Codify.Infrastructure.Execution;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
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
        private IThemeService _themeService;
        private IResourceServer _resourceServer;
        private IUserNotificationService _userNotificationService;

        public CodifyToolWindowControl()
        {
            try
            {
                InitializeComponent();

                Loaded += OnLoaded;
                Unloaded += OnUnloaded;
            }
            catch (Exception ex)
            {
                _ = HandleInitializationErrorAsync(ex, "Constructor");
            }

        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            try
            {
                // Initialize services that don't depend on WebView
                _themeService = CodifyServiceContainer.Get<IThemeService>();
                _resourceServer = CodifyServiceContainer.Get<IResourceServer>();

                SetupThemeIntegration();

                _userNotificationService = CodifyServiceContainer.Get<IUserNotificationService>();

                var pipeline = CodifyServiceContainer.Get<ExecutionPipeline>();

                _ = pipeline.RunAsync(
                    InitializeWebViewAsync,
                    nameof(InitializeWebViewAsync),
                    showMessageBox: true);
            }
            catch (Exception ex)
            {
                _ = HandleInitializationErrorAsync(ex, "CodifyToolWindowControl|OnLoaded");
            }
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

            await InitializeWebViewZoomAsync();

            WebView.CoreWebView2.Navigate(
                "http://codify.resources/Chat/view/chat-view.html"
            );
        }
        // Initialize WebView settings
        private async Task InitializeWebViewZoomAsync()
        {

            // Disable default browser zoom shortcuts (Ctrl + Plus/Minus/MouseWheel) if you want consistent scale
            WebView.CoreWebView2.Settings.IsZoomControlEnabled = false;

            // Detect screen width/height or DPI to apply scaling factor
            double scalingFactor = CalculateZoomFactorBasedOnScreen();

            // Set the WebView zoom factor (e.g., 0.9 for 90% size)
            WebView.ZoomFactor = scalingFactor;
        }

        private double CalculateZoomFactorBasedOnScreen()
        {
            // Get primary screen working area width (or use SystemParameters in WPF / Screen in WinForms)
            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;

            // If the monitor is smaller than full HD (1920px width), reduce the zoom factor
            if (screenWidth < 1366)
            {
                return 0.55; // 85% scale for small screens
            }
            if (screenWidth < 1980)
            {
                return 0.60; // 90% scale for laptop screens
            }

            return 1.0; // 100% scale for standard desktop monitors
        }

        private async Task RegisterWebViewHandlersAsync()
        {
            var errorHandler = CodifyServiceContainer.Get<IErrorHandler>();
            // 5. Subscribe to incoming messages from JS
            WebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;


            // Catch navigation errors (404, DNS, etc.)
            WebView.CoreWebView2.NavigationCompleted += (s, e) =>
            {
                if (e.IsSuccess) return;

                errorHandler.HandleUiError(
                    "WebView_Navigation",
                    "Ui_Navigation_Error",
                    $"Navigation failed with error: {e.WebErrorStatus}",
                    $"URL: {WebView.Source}"
                );

                _userNotificationService.ShowError(ErrorMessages.StartupError);
            };

            // Catch resource loading errors (Sub-resources like JS/CSS files)
            WebView.CoreWebView2.WebResourceResponseReceived += (s, e) =>
            {
                if (e.Response.StatusCode < 400) return;

                errorHandler.HandleUiError(
                    "WebView_Resource",
                    "Ui_ResourceResponse_Error",
                    $"Failed to load: {e.Request.Uri}",
                    $"Status Code: {e.Response.StatusCode}"
                );

                _userNotificationService.ShowError(ErrorMessages.StartupError);
            };
        }

        /// <summary>
        /// Handles UI initialization errors safely.
        /// This method must never throw because it is used during tool window creation.
        /// </summary>
        private static async Task HandleInitializationErrorAsync(
            Exception exception,
            string operationName)
        {
            try
            {
                if (CodifyServiceContainer.IsInitialized)
                {
                    try
                    {
                        var handler = CodifyServiceContainer.Get<IErrorHandler>();

                        handler.Handle(
                            exception,
                            $"CodifyToolWindowControl.{operationName}");
                    }
                    catch
                    {
                        Debug.WriteLine(exception);
                    }
                }
                else
                {
                    Debug.WriteLine("[Codify] UI initialization failed before DI initialization.");
                    Debug.WriteLine(exception);
                }

                try
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    VsShellUtilities.ShowMessageBox(
                        ServiceProvider.GlobalProvider,
                        $"Codify failed while executing '{operationName}'.\n\nPlease check the Output window for more details.",
                        "Codify",
                        OLEMSGICON.OLEMSGICON_CRITICAL,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                }
                catch
                {
                    // Never throw from the error handler.
                }
            }
            catch
            {
                Debug.WriteLine("[Codify] Failed to handle UI initialization error.");
                Debug.WriteLine(exception);
            }
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