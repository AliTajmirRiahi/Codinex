using System;
using Codinex.Core.Models.Documents;

namespace Codinex.Core.Interfaces.Documents
{
    public interface IActiveDocumentWatcher
    {
        event EventHandler<ActiveDocumentChangedEventArgs> ActiveDocumentChanged;
    }

}
