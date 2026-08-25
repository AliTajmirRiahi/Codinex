using System;
using Codinex.Core.Models;

namespace Codinex.Core.Interfaces.Documents
{
    public interface IActiveDocumentWatcher
    {
        event EventHandler<ActiveDocumentChangedEventArgs> ActiveDocumentChanged;
    }

}
