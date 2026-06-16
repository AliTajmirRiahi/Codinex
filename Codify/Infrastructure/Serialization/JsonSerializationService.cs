using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Codify.Core.Abstractions;
using Newtonsoft.Json.Linq;

namespace Codify.Infrastructure.Serialization;

public sealed class JsonSerializationService : IJsonSerializer
{
    public JsonSerializationService()
    {

    }
    private static readonly JsonSerializerSettings Settings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),

        // remove null properties
        NullValueHandling = NullValueHandling.Ignore,

        // enums -> "value" instead of 0
        Converters =
        {
            new StringEnumConverter()
        },

        Formatting = Formatting.None
    };

    public string Serialize(object obj)
    {
        return JsonConvert.SerializeObject(obj, Settings);
    }

    public T Deserialize<T>(string json)
    {
        return JsonConvert.DeserializeObject<T>(json, Settings);
    }

    public object Deserialize(string json, Type type)
    {
        return JsonConvert.DeserializeObject(json, type, Settings);
    }

    public JObject Parse(string json)
    {
        return JObject.Parse(json);
    }
}