using System;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiniMikuDance.Util;

/// <summary>
/// System.Numerics.Vector3 を { "X": value, "Y": value, "Z": value } 形式でシリアライズするためのコンバーター。
/// </summary>
public class Vector3JsonConverter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        float x = 0, y = 0, z = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return new Vector3(x, y, z);

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string? property = reader.GetString();
                if (!reader.Read())
                    throw new JsonException();

                switch (property)
                {
                    case "X":
                    case "x":
                        x = Sanitize(reader.GetSingle());
                        break;
                    case "Y":
                    case "y":
                        y = Sanitize(reader.GetSingle());
                        break;
                    case "Z":
                    case "z":
                        z = Sanitize(reader.GetSingle());
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException();
    }

    private static float Sanitize(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value) || value < -1000f || value > 1000f
            ? 0f
            : value;
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("X", value.X);
        writer.WriteNumber("Y", value.Y);
        writer.WriteNumber("Z", value.Z);
        writer.WriteEndObject();
    }
}
