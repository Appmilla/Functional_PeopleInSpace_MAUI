using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FunctionalPeopleInSpaceMaui.Models;

public partial class CrewModel(
    string name,
    string agency,
    Uri image,
    Uri wikipedia,
    IReadOnlyList<string> launches,
    Status status,
    string id)
{
    [JsonProperty("name")]
    public string Name { get; } = name;

    [JsonProperty("agency")]
    public string Agency { get; } = agency;

    [JsonProperty("image")]
    public Uri Image { get; } = image;

    [JsonProperty("wikipedia")]
    public Uri Wikipedia { get; } = wikipedia;

    [JsonProperty("launches")]
    public IReadOnlyList<string> Launches { get; } = launches;

    [JsonProperty("status")]
    public Status Status { get; } = status;

    [JsonProperty("id")]
    public string Id { get; } = id;
    
    public static CrewModel[] FromJson(string json) => JsonConvert.DeserializeObject<CrewModel[]>(json, Converter.Settings);
}

public enum Status { Active, Inactive, Retired, Unknown };

/*
public partial class CrewModel
{
    public static CrewModel Create(string name, string agency, Uri image, Uri wikipedia, IReadOnlyList<string> launches, Status status, string id)
    {
        return new CrewModel(name, agency, image, wikipedia, launches, status, id);
    }
}*/

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
            _ => throw new Exception("Cannot unmarshal type Status")
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
            _ => throw new Exception("Cannot marshal type Status")
        };
        serializer.Serialize(writer, statusString);
    }

    public static readonly StatusConverter Singleton = new();
}