using Codify.Core.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codify.Infrastructure.Serialization
{
    public sealed class NewtonsoftPayloadBinder : IPayloadBinder
    {
        private readonly JsonSerializer _serializer;

        public NewtonsoftPayloadBinder(JsonSerializer serializer)
        {
            _serializer = serializer;
        }

        public T Bind<T>(JObject payload)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));

            try
            {
                return payload.ToObject<T>(_serializer)
                       ?? throw new InvalidOperationException(
                           $"Could not bind payload to type {typeof(T).Name}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Payload binding failed for {typeof(T).Name}", ex);
            }
        }
    }
}
