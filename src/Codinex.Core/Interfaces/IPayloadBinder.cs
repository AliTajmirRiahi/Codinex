using Newtonsoft.Json.Linq;

namespace Codinex.Core.Interfaces
{
    public interface IPayloadBinder
    {
        T Bind<T>(JObject payload);
    }

}
