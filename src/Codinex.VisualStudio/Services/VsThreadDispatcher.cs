using Microsoft.VisualStudio.Shell;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;

namespace Codinex.VisualStudio.Services
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
