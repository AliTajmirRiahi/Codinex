using System;
using System.Threading.Tasks;

namespace Codify.Core.Interfaces
{
    public interface IExecutionPipeline
    {
        Task RunAsync(
            Func<Task> action,
            string source,
            bool showMessageBox = false);

        Task<T> RunAsync<T>(
            Func<Task<T>> action,
            string source,
            T fallback,
            bool showMessageBox = false);
    }
}
