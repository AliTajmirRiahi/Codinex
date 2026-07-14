using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Conversation;
using Codify.Core.Tools;
using Newtonsoft.Json.Linq;

namespace Codify.Infrastructure.Tools;

/// <summary>
/// Simple tool used to validate the conversation runtime.
/// </summary>
public sealed class PingTool : IAiTool
{
    public string Name => "ping";

    public Task<ToolResult> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ToolResult
        {
            Success = true,
            Data = JObject.FromObject(new
            {
                message = "pong"
            })
        });
    }
}