using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codify.Core.Abstractions
{
    public interface IResourceServer
    {
        void Attach(CoreWebView2 webview);
    }
}
