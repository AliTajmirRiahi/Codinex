using System;


namespace Codinex.Core.Models
{
    public sealed class WorkspaceFileChangedEventArgs : EventArgs
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
    }

}
