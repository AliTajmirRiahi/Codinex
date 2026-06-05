using System;
using Codify.Core.Abstractions;
using Microsoft.Web.WebView2.Core;
using System.Reflection;

namespace Codify.Infrastructure.WebView
{
    /// <summary>
    /// Serves all embedded files under the Resources folder
    /// through the virtual host: http://codify.resources/*
    /// </summary>
    public class WebViewResourceServer : IResourceServer

    {
        private readonly Assembly _assembly;
        private readonly string _resourceRoot;

        public WebViewResourceServer(Assembly assembly, string resourceRoot)
        {
            _assembly = assembly;
            _resourceRoot = resourceRoot;
        }

        public void Attach(CoreWebView2 webview)
        {
            webview.AddWebResourceRequestedFilter(
                "http://codify.resources/*",
                CoreWebView2WebResourceContext.All
            );

            webview.WebResourceRequested += HandleRequest;
        }

        private void HandleRequest(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            var uri = new Uri(e.Request.Uri);

            if (uri.Host != "codify.resources")
                return;

            // convert url path → embedded resource name
            string relativePath = uri.AbsolutePath.TrimStart('/').Replace("/", ".");

            string resourceName = $"{_resourceRoot}.{relativePath}";

            var stream = _assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
                return;

            string contentType = GetContentType(resourceName);

            var webview = (CoreWebView2)sender;

            e.Response = webview.Environment.CreateWebResourceResponse(
                stream,
                200,
                "OK",
                $"Content-Type: {contentType}"
            );
        }

        private static string GetContentType(string path)
        {
            if (path.EndsWith(".html")) return "text/html";
            if (path.EndsWith(".css")) return "text/css";
            if (path.EndsWith(".js")) return "application/javascript";
            if (path.EndsWith(".svg")) return "image/svg+xml";
            if (path.EndsWith(".png")) return "image/png";
            if (path.EndsWith(".woff")) return "font/woff";
            if (path.EndsWith(".woff2")) return "font/woff2";

            return "text/plain";
        }
    }
}
