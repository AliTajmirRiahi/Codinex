using Codify.Core.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codify.Infrastructure.Serialization
{
    public sealed class NewtonsoftPayloadBinder(JsonSerializer serializer) : IPayloadBinder
    {
        public T Bind<T>(JObject payload)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));

            return payload.ToObject<T>(serializer)
                   ?? throw new InvalidOperationException(
                       $"Could not bind payload to type {typeof(T).Name}");
        }
    }
}
