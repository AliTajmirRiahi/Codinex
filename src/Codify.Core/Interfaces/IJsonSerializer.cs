using Newtonsoft.Json.Linq;
using System;
using System.Xml.Linq;

namespace Codify.Core.Interfaces
{
    public interface IJsonSerializer
    {
        string Serialize(object obj);

        T Deserialize<T>(string json);

        object Deserialize(string json, Type type);

        JObject Parse(string json);
    }
}

