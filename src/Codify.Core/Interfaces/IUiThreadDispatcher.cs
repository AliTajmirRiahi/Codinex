using System.Threading.Tasks;

namespace Codify.Core.Interfaces
{
    public interface IUiThreadDispatcher
    {
        Task SwitchToMainThreadAsync();

        void ThrowIfNotOnUIThread();
    }
}
