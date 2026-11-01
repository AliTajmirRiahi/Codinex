using System;
using System.Threading.Tasks;
using Codinex.Core.Interfaces.Services;
using Microsoft.Web.WebView2.Wpf;

namespace Codinex.UI.ToolWindows
{
    /// <summary>
    /// Keeps a tool window's WebView2 at the zoom level held by <see cref="IUiZoomService"/>.
    /// WebView2 handles the Ctrl + mouse wheel gesture itself; this binder clamps the factor it
    /// produces, writes it back to the settings, and applies a level chosen somewhere else
    /// (the Settings field, or another Codinex tool window) to this WebView.
    /// </summary>
    internal sealed class WebViewZoomBinder : IDisposable
    {
        private readonly WebView2 _webView;
        private readonly IUiZoomService _zoomService;
        private readonly IExecutionPipeline _pipeline;
        private readonly IUiThreadDispatcher _uiThreadDispatcher;

        // Set while this binder is the one writing ZoomFactor, so the resulting
        // ZoomFactorChanged is not mistaken for a user gesture and persisted again.
        private bool _isApplying;
        private bool _isDisposed;

        public WebViewZoomBinder(
            WebView2 webView,
            IUiZoomService zoomService,
            IExecutionPipeline pipeline,
            IUiThreadDispatcher uiThreadDispatcher)
        {
            _webView = webView;
            _zoomService = zoomService;
            _pipeline = pipeline;
            _uiThreadDispatcher = uiThreadDispatcher;

            // Let WebView2 own Ctrl + mouse wheel and Ctrl + Plus/Minus instead of locking the
            // panel to a fixed scale; the result is clamped and persisted below.
            _webView.CoreWebView2.Settings.IsZoomControlEnabled = true;

            ApplyZoomFactor(_zoomService.ZoomPercent);

            _webView.ZoomFactorChanged += OnZoomFactorChanged;
            _zoomService.ZoomPercentChanged += OnZoomPercentChanged;
        }

        private void OnZoomFactorChanged(object sender, EventArgs e)
        {
            if (_isDisposed || _isApplying) return;

            var percent = ToPercent(_webView.ZoomFactor);
            var clamped = _zoomService.Clamp(percent);

            // A wheel tick past the supported range is pinned to the edge rather than honoured.
            if (clamped != percent)
            {
                ApplyZoomFactor(clamped);
            }

            if (clamped == _zoomService.ZoomPercent) return;

            _ = _pipeline.RunAsync(
                () => _zoomService.SetZoomPercentAsync(clamped),
                nameof(WebViewZoomBinder));
        }

        private void OnZoomPercentChanged(object sender, int percent)
        {
            if (_isDisposed) return;

            _ = _pipeline.RunAsync(
                async () =>
                {
                    await _uiThreadDispatcher.SwitchToMainThreadAsync();

                    if (_isDisposed || ToPercent(_webView.ZoomFactor) == percent) return;

                    ApplyZoomFactor(percent);
                },
                nameof(WebViewZoomBinder));
        }

        private void ApplyZoomFactor(int percent)
        {
            _isApplying = true;

            try
            {
                _webView.ZoomFactor = percent / 100.0;
            }
            finally
            {
                _isApplying = false;
            }
        }

        private static int ToPercent(double zoomFactor) => (int)Math.Round(zoomFactor * 100);

        public void Dispose()
        {
            if (_isDisposed) return;

            _isDisposed = true;

            _webView.ZoomFactorChanged -= OnZoomFactorChanged;
            _zoomService.ZoomPercentChanged -= OnZoomPercentChanged;
        }
    }
}
