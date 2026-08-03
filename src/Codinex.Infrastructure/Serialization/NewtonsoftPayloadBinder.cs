using Codify.Core.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;

namespace Codify.Infrastructure.Serialization
{

    [AutoDiRegister(Modules.JSON, RegistrationOrder.Foundation)]
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
