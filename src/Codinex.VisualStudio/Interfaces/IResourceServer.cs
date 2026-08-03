using Microsoft.Web.WebView2.Core;

namespace Codinex.VisualStudio.Interfaces
{
    public interface IResourceServer
    {
        void Attach(CoreWebView2 webview);
    }
}
