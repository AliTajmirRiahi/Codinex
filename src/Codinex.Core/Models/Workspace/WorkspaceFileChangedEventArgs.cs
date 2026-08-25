using System;


namespace Codinex.Core.Models.Workspace
{
    public sealed class WorkspaceFileChangedEventArgs : EventArgs
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
    }

}
