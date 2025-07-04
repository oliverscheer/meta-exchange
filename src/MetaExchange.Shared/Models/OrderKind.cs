using System.Text.Json.Serialization;

namespace MetaExchange.Shared.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderKind
{
    Limit
}
