using Microsoft.Web.WebView2.Core;

namespace Codify.VisualStudio.Interfaces
{
    public interface IResourceServer
    {
        void Attach(CoreWebView2 webview);
    }
}
