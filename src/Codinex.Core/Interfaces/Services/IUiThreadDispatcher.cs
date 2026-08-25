using System.Threading.Tasks;

namespace Codinex.Core.Interfaces.Services
{
    public interface IUiThreadDispatcher
    {
        Task SwitchToMainThreadAsync();

        void ThrowIfNotOnUIThread();
    }
}
