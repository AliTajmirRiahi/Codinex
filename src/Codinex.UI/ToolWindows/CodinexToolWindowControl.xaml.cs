using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Codinex.Core.Interfaces.Services;
using Codinex.Core.Models.WebView;
using Codinex.VisualStudio.CommitMessages;
using Codinex.VisualStudio.Diagnostics.Errors;
using Codinex.VisualStudio.Interfaces;

namespace Codinex.UI.ToolWindows
{
    /// <summary>
    /// Interaction logic for CodinexToolWindowControl.
    /// </summary>
    public partial class CodinexToolWindowControl : UserControl, IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodinexToolWindowControl"/> class.
        /// </summary>
        private IServiceProvider _serviceProvider;
        private IThemeService _themeService;
        private IResourceServer _resourceServer;
        private IUserNotificationService _userNotificationService;
        private IWebViewClient _webViewClient;
        private IUiZoomService _uiZoomService;
        private WebViewZoomBinder _zoomBinder;
        private static IErrorHandler _errorHandler;
        private bool _isDisposed;

        public CodinexToolWindowControl()
        {
            try
            {
                InitializeComponent();

                Loaded += OnLoaded;
                Unloaded += OnUnloaded;
                IsVisibleChanged += OnIsVisibleChanged;
            }
            catch (Exception ex)
            {
                _ = HandleInitializationErrorAsync(ex, "Constructor");
            }
        }

        public void Initialize(IServiceProvider serviceProvider, IErrorHandler errorHandler)
        {
            _serviceProvider = serviceProvider;
            _errorHandler = errorHandler;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            GitCommitButtonVisibility.SetCodinexOpen(true);

            try
            {
                // Initialize services that don't depend on WebView
                _themeService = _serviceProvider.GetRequiredService<IThemeService>();
                _resourceServer = _serviceProvider.GetRequiredService<IResourceServer>();

                SetupThemeIntegration();

                _userNotificationService = _serviceProvider.GetRequiredService<IUserNotificationService>();

                var pipeline = _serviceProvider.GetRequiredService<IExecutionPipeline>();

                _ = pipeline.RunAsync(
                    InitializeWebViewAsync,
                    nameof(InitializeWebViewAsync),
                    showMessageBox: true);
            }
            catch (Exception ex)
            {
                _ = HandleInitializationErrorAsync(ex, "CodinexToolWindowControl|OnLoaded");
            }
        }

        private async Task InitializeWebViewAsync()
        {
            if (_isDisposed) return;

            // Define the user data folder path to avoid permission issues.
            var userDataFolder = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "CodinexExtension");

            // Create the WebView2 environment using the custom folder.
            var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

            // Initialize WebView2 with the environment.
            await WebView.EnsureCoreWebView2Async(environment);

            if (_isDisposed) return;

#if DEBUG
            WebView.CoreWebView2.OpenDevToolsWindow();

            // Developer-only UI (e.g. the Open prompt folder button) is gated on this flag.
            await WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync("window.CODINEX_DEBUG = true;");
#endif

            // Get services from our DI Container
            _webViewClient = _serviceProvider.GetRequiredService<IWebViewClient>();

            _webViewClient.Initialize(WebView);

            WebViewChromeSettings.DisableBrowserChrome(WebView.CoreWebView2);

            // Set up the resource server mapping
            _resourceServer.Attach(WebView.CoreWebView2);

            await RegisterWebViewHandlersAsync();

            InitializeWebViewZoom();

            WebView.CoreWebView2.Navigate(
                "http://codinex.resources/Chat/view/chat-view.html"
            );
        }
        /// <summary>
        /// Puts the panel at the zoom level the user last chose and keeps it there. The scale
        /// used to be guessed from the primary screen width, which shrank the panel to 60% on
        /// any monitor narrower than 1980px and left the text unreadably small.
        /// </summary>
        private void InitializeWebViewZoom()
        {
            _uiZoomService = _serviceProvider.GetRequiredService<IUiZoomService>();

            _zoomBinder = new WebViewZoomBinder(
                WebView,
                _uiZoomService,
                _serviceProvider.GetRequiredService<IExecutionPipeline>(),
                _serviceProvider.GetRequiredService<IUiThreadDispatcher>());

            // Ctrl + mouse wheel changes the level behind the chat UI's back; tell it so the
            // zoom field in Codinex Settings opens on the real value instead of a stale one.
            _uiZoomService.ZoomPercentChanged += OnUiZoomPercentChanged;
        }

        private void OnUiZoomPercentChanged(object sender, int percent)
        {
            if (_isDisposed || _webViewClient == null) return;

            var pipeline = _serviceProvider.GetRequiredService<IExecutionPipeline>();

            _ = pipeline.RunAsync(
                () => _webViewClient.PostMessageAsync(new WebViewMessageResponse
                {
                    Type = WebViewMessageType.UiZoomChanged,
                    Payload = new { UiZoomPercent = percent },
                    Timestamp = DateTime.Now
                }),
                nameof(OnUiZoomPercentChanged));
        }

        private async Task RegisterWebViewHandlersAsync()
        {
            var errorHandler = _serviceProvider.GetRequiredService<IErrorHandler>();
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
                try
                {
                    _errorHandler.Handle(
                         exception,
                         $"CodinexToolWindowControl.{operationName}");
                }
                catch
                {
                    Debug.WriteLine(exception);
                }

                try
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    VsShellUtilities.ShowMessageBox(
                        Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider,
                        $"Codinex failed while executing '{operationName}'.\n\nPlease check the Output window for more details.",
                        "Codinex",
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
                Debug.WriteLine("[Codinex] Failed to handle UI initialization error.");
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
            var router = _serviceProvider.GetRequiredService<IWebViewMessageRouter>();
            var pipeline = _serviceProvider.GetRequiredService<IExecutionPipeline>();

            _ = pipeline.RunAsync(
                () => router.HandleMessageAsync(e.WebMessageAsJson),
                "WebViewMessageRouter");
        }
        private void OnThemeChanged(object sender, EventArgs e)
        {
            _ = ApplyThemeToWebViewAsync();
        }
        private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            _ = ApplyThemeToWebViewAsync();
        }

        private async Task ApplyThemeToWebViewAsync()
        {
            if (_isDisposed || WebView.CoreWebView2 == null) return;

            var cssVariables = _themeService.GetCurrentThemeAsCssVariables();

            await WebView.CoreWebView2.ExecuteScriptAsync(cssVariables);
        }
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Visual Studio can unload tool window content during docking, auto-hide,
            // or unpin operations. None of those mean Codinex was closed, so do NOT
            // clear the Git commit-button visibility flag here (that would drop the
            // injected "Generate Commit Message" button and it would not come back on
            // re-pin). Do not dispose WebView here either; both the flag and the
            // WebView are only torn down in Dispose(), when the tool window pane is
            // actually destroyed.
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Only re-assert "open" when the window becomes visible again (e.g. an
            // auto-hidden/unpinned pane flying back out). Becoming invisible is not a
            // close — Dispose() is the single source of truth for that.
            if (IsVisible)
            {
                GitCommitButtonVisibility.SetCodinexOpen(true);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            _isDisposed = true;

            if (!disposing) return;

            Loaded -= OnLoaded;
            Unloaded -= OnUnloaded;
            IsVisibleChanged -= OnIsVisibleChanged;
            GitCommitButtonVisibility.SetCodinexOpen(false);

            if (_themeService != null)
            {
                _themeService.ThemeChanged -= OnThemeChanged;
            }

            if (_uiZoomService != null)
            {
                _uiZoomService.ZoomPercentChanged -= OnUiZoomPercentChanged;
            }

            _zoomBinder?.Dispose();

            if (WebView == null) return;

            WebView.NavigationCompleted -= OnNavigationCompleted;

            if (WebView.CoreWebView2 != null)
            {
                WebView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
            }

            WebView.Dispose();
        }
    }
}