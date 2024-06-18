using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FunctionalPeopleInSpaceMaui.Models;

public record CrewModel(
    [property: JsonProperty("name")] string Name,
    [property: JsonProperty("agency")] string Agency,
    [property: JsonProperty("image")] Uri Image,
    [property: JsonProperty("wikipedia")] Uri Wikipedia,
    [property: JsonProperty("launches")] IReadOnlyList<string> Launches,
    [property: JsonProperty("status")] Status Status,
    [property: JsonProperty("id")] string Id)
{
    public static Either<CrewError, CrewModel[]> FromJson(string json)
    {
        try
        {
            var models = JsonConvert.DeserializeObject<CrewModel[]>(json, Converter.Settings);
            return models == null 
                ? Either<CrewError, CrewModel[]>.Left(new ParsingError("Deserialization returned null.")) 
                : Either<CrewError, CrewModel[]>.Right(models);
        }
        catch (JsonException ex)
        {
            return Either<CrewError, CrewModel[]>.Left(new ParsingError("Failed to parse crew data: " + ex.Message));
        }
    }
}

public enum Status { Active, Inactive, Retired, Unknown };

internal static class Converter
{
    public static readonly JsonSerializerSettings Settings = new()
    {
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        DateParseHandling = DateParseHandling.None,
        Converters =
        {
            StatusConverter.Singleton,
            new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
        },
    };
}

internal class StatusConverter : JsonConverter
{
    public override bool CanConvert(Type t) => t == typeof(Status) || t == typeof(Status?);

    public override object? ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return null;
        var value = serializer.Deserialize<string>(reader);
        return value switch
        {
            "active" => Status.Active,
            "inactive" => Status.Inactive,
            "retired" => Status.Retired,
            "unknown" => Status.Unknown,
            _ => throw new JsonException("Cannot unmarshal type Status")
        };
    }

    public override void WriteJson(JsonWriter writer, object? untypedValue, JsonSerializer serializer)
    {
        if (untypedValue == null)
        {
            serializer.Serialize(writer, null);
            return;
        }
        var value = (Status)untypedValue;
        string statusString = value switch
        {
            Status.Active => "active",
            Status.Inactive => "inactive",
            Status.Retired => "retired",
            Status.Unknown => "unknown",
            _ => throw new JsonException("Cannot marshal type Status")
        };
        serializer.Serialize(writer, statusString);
    }

    public static readonly StatusConverter Singleton = new();
}