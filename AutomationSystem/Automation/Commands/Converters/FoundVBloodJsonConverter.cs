using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using CrowbaneArena.Data;

namespace CrowbaneArena.Commands.Converters
{
    internal class FoundVBloodJsonConverter : JsonConverter<FoundVBlood>
    {
        public override FoundVBlood Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException();
            }

            var name = reader.GetString();

            if (name.StartsWith("Primal "))
            {
                // Handle primal variants if needed
                if (FoundVBloodConverter.Parse(name.Substring(7), out var foundPrimal))
                {
                    return new FoundVBlood(foundPrimal.Value, name);
                }
            }

            if(FoundVBloodConverter.Parse(name, out var foundVBlood))
            {
                return foundVBlood;
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, FoundVBlood value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Name);
        }
    }
}
