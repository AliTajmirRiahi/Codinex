using System;


namespace Codify.Core.Models
{
    public sealed class ActiveDocumentChangedEventArgs : EventArgs
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
    }

}
