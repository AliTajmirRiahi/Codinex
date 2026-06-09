using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codify.Core.Abstractions
{
    public interface IPayloadBinder
    {
        T Bind<T>(JObject payload);
    }

}
