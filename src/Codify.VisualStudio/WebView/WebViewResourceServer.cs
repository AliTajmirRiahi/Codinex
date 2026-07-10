using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using Codify.VisualStudio.Interfaces;

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

            var webview = (CoreWebView2)sender;

            // convert url path → embedded resource name
            string relativePath = uri.AbsolutePath.TrimStart('/').Replace("/", ".");

            string resourceName = $"{_resourceRoot}.{relativePath}";

            var stream = _assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                // Return a real 404 response instead of throwing.
                var notFoundStream = new MemoryStream(Encoding.UTF8.GetBytes($"Resource not found {resourceName}."));

                e.Response = webview.Environment.CreateWebResourceResponse(
                    notFoundStream,
                    404,
                    "Not Found",
                    "Content-Type: text/plain"
                );

                return;
            }

            string contentType = GetContentType(resourceName);

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
