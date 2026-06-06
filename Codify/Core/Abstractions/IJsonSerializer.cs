using System;

namespace Codify.Core.Abstractions;

public interface IJsonSerializer
{
    string Serialize(object obj);

    T Deserialize<T>(string json);

    object Deserialize(string json, Type type);
}