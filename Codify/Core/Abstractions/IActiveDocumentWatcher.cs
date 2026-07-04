using Codify.Core.Models;
using System;

namespace Codify.Core.Abstractions
{
    public interface IActiveDocumentWatcher
    {
        event EventHandler<ActiveDocumentChangedEventArgs> ActiveDocumentChanged;
    }

}
