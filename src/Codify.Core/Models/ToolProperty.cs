using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace Codify.Core.Models
{
    public enum ToolPropertyType
    {
        String,
        Integer,
        Number,
        Boolean,
        Array,
        Object
    }

    public sealed class ToolProperty
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ToolPropertyType Type { get; }

        public string Description { get; }

        public ToolProperty(
            ToolPropertyType type,
            string description)
        {
            Type = type;
            Description = description;
        }
    }
}
