using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Unity.Mathematics;

namespace CrowbaneArena.JsonConverters
{
    /// <summary>
    /// System.Text.Json converter for Unity.Mathematics.float2
    /// JSON shape: [x, y]
    /// </summary>
    public sealed class Float2JsonConverter : JsonConverter<float2>
    {
        public override float2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return float2.zero;
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("float2 should be a JSON array: [x, y]");

            // [ x , y ]
            reader.Read();
            if (reader.TokenType != JsonTokenType.Number) throw new JsonException("float2 x must be a number");
            var x = reader.GetSingle();

            reader.Read();
            if (reader.TokenType != JsonTokenType.Number) throw new JsonException("float2 y must be a number");
            var y = reader.GetSingle();

            // Move past EndArray
            reader.Read();

            return new float2(x, y);
        }

        public override void Write(Utf8JsonWriter writer, float2 value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.x);
            writer.WriteNumberValue(value.y);
            writer.WriteEndArray();
        }
    }
}
