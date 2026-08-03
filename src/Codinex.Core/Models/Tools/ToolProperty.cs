using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Codinex.Core.Models.Tools
{
    public enum ToolPropertyType
    {
        String,
        Integer,
        Number,
        Boolean,
        Array,
        Object,
    }

    public sealed class ToolProperty(
        ToolPropertyType type,
        string description)
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ToolPropertyType Type { get; set; } = type;

        public string Description { get; set; } = description;

        public Dictionary<string, ToolProperty> Properties { get; set; }

        public ToolProperty Items { get; set; }

        public IReadOnlyList<string> Required { get; set; }

        public IReadOnlyList<string> Enum { get; set; }

        // NEW
        public IReadOnlyList<ToolProperty> OneOf { get; set; }

        // optional
        public IReadOnlyList<ToolProperty> AnyOf { get; set; }


        public object ToJsonSchema()
        {
            var schema = new Dictionary<string, object>
            {
                ["type"] = ToJsonSchemaType(Type)
            };

            if (!string.IsNullOrWhiteSpace(Description))
            {
                schema["description"] = Description;
            }

            if (Properties?.Count > 0)
            {
                schema["properties"] = Properties.ToDictionary(
                    p => p.Key,
                    p => p.Value.ToJsonSchema());
            }

            if (Required?.Count > 0)
            {
                schema["required"] = Required;
            }

            if (Items != null)
            {
                schema["items"] = Items.ToJsonSchema();
            }

            if (Enum?.Count > 0)
            {
                schema["enum"] = Enum;
            }

            if (OneOf?.Count > 0)
            {
                schema["oneOf"] = OneOf
                    .Select(p => p.ToJsonSchema())
                    .ToArray();
            }

            if (AnyOf?.Count > 0)
            {
                schema["anyOf"] = AnyOf
                    .Select(p => p.ToJsonSchema())
                    .ToArray();
            }


            return schema;
        }

        private static string ToJsonSchemaType(
            ToolPropertyType type)
        {
            return type switch
            {
                ToolPropertyType.String => "string",
                ToolPropertyType.Integer => "integer",
                ToolPropertyType.Number => "number",
                ToolPropertyType.Boolean => "boolean",
                ToolPropertyType.Object => "object",
                ToolPropertyType.Array => "array",
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }
    }
}
