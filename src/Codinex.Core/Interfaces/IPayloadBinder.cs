using Newtonsoft.Json.Linq;

namespace Codify.Core.Interfaces
{
    public interface IPayloadBinder
    {
        T Bind<T>(JObject payload);
    }

}
