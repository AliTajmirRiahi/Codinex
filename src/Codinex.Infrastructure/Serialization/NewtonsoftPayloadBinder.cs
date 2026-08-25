using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Services;
namespace Codinex.Infrastructure.Serialization
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
