using System;
using System.Threading.Tasks;
using Codinex.Core.Interfaces;

namespace Codinex.TestCommon.Fakes
{
    public sealed class TestExecutionPipeline : IExecutionPipeline
    {
        public int RunCount { get; private set; }

        public async Task RunAsync(
            Func<Task> action,
            string source,
            bool showMessageBox = false)
        {
            RunCount++;

            await action();
        }

        public async Task<T> RunAsync<T>(
            Func<Task<T>> action,
            string source,
            T fallback,
            bool showMessageBox = false)
        {
            RunCount++;

            return await action();
        }
    }
}