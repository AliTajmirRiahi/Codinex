using System.Threading.Tasks;

namespace Codinex.Core.Interfaces
{
    public interface IUiThreadDispatcher
    {
        Task SwitchToMainThreadAsync();

        void ThrowIfNotOnUIThread();
    }
}
