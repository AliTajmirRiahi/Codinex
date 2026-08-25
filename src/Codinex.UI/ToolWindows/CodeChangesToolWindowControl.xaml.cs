using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Codinex.Core.Interfaces.Services;
using Codinex.VisualStudio.Diagnostics.Errors;
using Codinex.VisualStudio.Interfaces;
using Codinex.VisualStudio.Tools.BuiltIn.Workspace;

namespace Codinex.UI.ToolWindows
{
    /// <summary>
    /// Interaction logic for CodeChangesToolWindowControl.
    /// </summary>
    public partial class CodeChangesToolWindowControl : UserControl, IDisposable
    {
        private IServiceProvider _serviceProvider;
        private IThemeService _themeService;
        private IResourceServer _resourceServer;
        private IUserNotificationService _userNotificationService;
        private static IErrorHandler _errorHandler;
        private bool _isDisposed;

        public CodeChangesToolWindowControl()
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

        public void Initialize(IServiceProvider serviceProvider, IErrorHandler errorHandler)
        {
            _serviceProvider = serviceProvider;
            _errorHandler = errorHandler;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            try
            {
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
                _ = HandleInitializationErrorAsync(ex, "CodeChangesToolWindowControl|OnLoaded");
            }
        }

        private async Task InitializeWebViewAsync()
        {
            if (_isDisposed) return;

            var userDataFolder = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "CodinexExtension");

            var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

            await WebView.EnsureCoreWebView2Async(environment);

            if (_isDisposed) return;

            var webViewClient = _serviceProvider.GetRequiredService<IChangeReviewWebViewClient>();

            webViewClient.Initialize(WebView);

            WebViewChromeSettings.DisableBrowserChrome(WebView.CoreWebView2);

            _resourceServer.Attach(WebView.CoreWebView2);

            await RegisterWebViewHandlersAsync();

            WebView.CoreWebView2.Navigate(
                "http://codinex.resources/ChangeReview/view/change-review-view.html"
            );
        }

        private async Task RegisterWebViewHandlersAsync()
        {
            var errorHandler = _serviceProvider.GetRequiredService<IErrorHandler>();

            WebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

            WebView.CoreWebView2.NavigationCompleted += (s, e) =>
            {
                if (e.IsSuccess) return;

                errorHandler.HandleUiError(
                    "ChangeReviewWebView_Navigation",
                    "Ui_Navigation_Error",
                    $"Navigation failed with error: {e.WebErrorStatus}",
                    $"URL: {WebView.Source}"
                );

                _userNotificationService.ShowError(ErrorMessages.StartupError);
            };

            WebView.CoreWebView2.WebResourceResponseReceived += (s, e) =>
            {
                if (e.Response.StatusCode < 400) return;

                errorHandler.HandleUiError(
                    "ChangeReviewWebView_Resource",
                    "Ui_ResourceResponse_Error",
                    $"Failed to load: {e.Request.Uri}",
                    $"Status Code: {e.Response.StatusCode}"
                );

                _userNotificationService.ShowError(ErrorMessages.StartupError);
            };
        }

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
                         $"CodeChangesToolWindowControl.{operationName}");
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
            WebView.NavigationCompleted += OnNavigationCompleted;

            _themeService.ThemeChanged += OnThemeChanged;
        }

        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var router = _serviceProvider.GetRequiredService<IChangeReviewMessageRouter>();
            var pipeline = _serviceProvider.GetRequiredService<IExecutionPipeline>();

            _ = pipeline.RunAsync(
                () => router.HandleMessageAsync(e.WebMessageAsJson),
                "ChangeReviewWebViewMessageRouter");
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
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
            if (_isDisposed || WebView.CoreWebView2 == null) return;

            var cssVariables = _themeService.GetCurrentThemeAsCssVariables();

            await WebView.CoreWebView2.ExecuteScriptAsync(cssVariables);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Visual Studio can unload tool window content during docking, auto-hide,
            // or unpin operations. Do not dispose WebView here; dispose only when the
            // tool window pane is actually disposed.
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

            // The tool window closing does not mean the pending review was decided — it's still
            // pending (persisted, chat still blocked), it just has no live wait to resolve anymore.
            _serviceProvider?.GetService<IChangesetSessionService>()?.MarkUndecidedIfPending();

            Loaded -= OnLoaded;
            Unloaded -= OnUnloaded;

            if (_themeService != null)
            {
                _themeService.ThemeChanged -= OnThemeChanged;
            }

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
