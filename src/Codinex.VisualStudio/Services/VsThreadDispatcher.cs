using Codify.Core.Interfaces;
using Microsoft.VisualStudio.Shell;
using System.Threading.Tasks;
using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;

namespace Codify.VisualStudio.Services
{
    [AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Foundation)]
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
