using Codify.Core.Interfaces;
using Microsoft.VisualStudio.Shell;
using System.Threading.Tasks;

namespace Codify.VisualStudio.Services
{
    public sealed class VsThreadDispatcher : IUiThreadDispatcher
    {
        public async Task SwitchToMainThreadAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        }

        public void ThrowIfNotOnUIThread()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
        }
    }
}
