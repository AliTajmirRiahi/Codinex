using System;
using System.Threading.Tasks;
using Codify.Core.Interfaces;

namespace Codify.TestCommon.Fakes
{
    public sealed class TestExecutionPipeline : IExecutionPipeline
    {
        public Task RunAsync(
            Func<Task> action,
            string source,
            bool showMessageBox = false)
        {
            return action();
        }

        public Task<T> RunAsync<T>(
            Func<Task<T>> action,
            string source,
            T fallback,
            bool showMessageBox = false)
        {
            return action();
        }
    }
}