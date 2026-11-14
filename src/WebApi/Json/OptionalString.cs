using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApi.Json;

internal readonly struct OptionalString
{
    public bool IsSpecified { get; init; }

    public string? Value { get; init; }
}

internal sealed class OptionalStringJsonConverter : JsonConverter<OptionalString>
{
    public override OptionalString Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return new OptionalString { IsSpecified = true, Value = null };
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Global role must be a string or null.");
        }

        return new OptionalString { IsSpecified = true, Value = reader.GetString() };
    }

    public override void Write(Utf8JsonWriter writer, OptionalString value, JsonSerializerOptions options)
    {
        if (!value.IsSpecified || value.Value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value);
    }
}
