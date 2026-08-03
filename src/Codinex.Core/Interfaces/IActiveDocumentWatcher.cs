using System;
using Codinex.Core.Models;

namespace Codinex.Core.Interfaces
{
    public interface IActiveDocumentWatcher
    {
        event EventHandler<ActiveDocumentChangedEventArgs> ActiveDocumentChanged;
    }

}
