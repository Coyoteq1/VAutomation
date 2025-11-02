using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Unity.Mathematics;

namespace CrowbaneArena.Utils
{
    public class Float3JsonConverter : JsonConverter<float3>
    {
        public override float3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            float x = 0, y = 0, z = 0;
            
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return new float3(x, y, z);

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propertyName = reader.GetString();
                    reader.Read();
                    
                    switch (propertyName)
                    {
                        case "x":
                            x = reader.GetSingle();
                            break;
                        case "y":
                            y = reader.GetSingle();
                            break;
                        case "z":
                            z = reader.GetSingle();
                            break;
                    }
                }
            }
            
            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, float3 value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.x);
            writer.WriteNumber("y", value.y);
            writer.WriteNumber("z", value.z);
            writer.WriteEndObject();
        }
    }
}