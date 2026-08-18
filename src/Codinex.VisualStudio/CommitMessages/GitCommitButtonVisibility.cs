using System;

namespace Codinex.VisualStudio.CommitMessages
{
    public static class GitCommitButtonVisibility
    {
        private static bool _isCodinexOpen;

        public static event Action Changed;

        public static bool IsCodinexOpen => _isCodinexOpen;

        public static void SetCodinexOpen(bool isOpen)
        {
            if (_isCodinexOpen == isOpen) return;

            _isCodinexOpen = isOpen;
            Changed?.Invoke();
        }
    }
}
