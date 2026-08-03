using System;
using Codify.Core.Models;

namespace Codify.Core.Interfaces
{
    public interface IActiveDocumentWatcher
    {
        event EventHandler<ActiveDocumentChangedEventArgs> ActiveDocumentChanged;
    }

}
