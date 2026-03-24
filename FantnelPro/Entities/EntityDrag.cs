using System.Text.Json.Serialization;

namespace FantnelPro.Entities;

public class EntityDrag {
    [JsonPropertyName("sx")]
    public required int Sx { get; set; }

    [JsonPropertyName("sy")]
    public required int Sy { get; set; }
}