using System;
using System.IO;
using System.Reflection;
using System.Text;
using Codinex.VisualStudio.Interfaces;
using Microsoft.Web.WebView2.Core;

namespace Codinex.VisualStudio.WebView
{
    /// <summary>
    /// Serves all embedded files under the Resources folder
    /// through the virtual host: http://codinex.resources/*
    /// </summary>
    public class WebViewResourceServer(Assembly assembly, string resourceRoot) : IResourceServer

    {
        public void Attach(CoreWebView2 webview)
        {
            webview.AddWebResourceRequestedFilter(
                "http://codinex.resources/*",
                CoreWebView2WebResourceContext.All
            );

            webview.WebResourceRequested += HandleRequest;
        }

        private void HandleRequest(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            var uri = new Uri(e.Request.Uri);

            if (uri.Host != "codinex.resources")
                return;

            var webview = (CoreWebView2)sender;

            // convert url path → embedded resource name
            string relativePath = uri.AbsolutePath.TrimStart('/').Replace("/", ".");

            string resourceName = $"{resourceRoot}.{relativePath}";

            var stream = assembly.GetManifestResourceStream(resourceName);

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
