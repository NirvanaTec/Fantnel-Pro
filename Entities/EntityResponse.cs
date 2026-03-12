using System.Text.Json.Serialization;

namespace FantnelPro.Entities;

public class EntityResponseBase {
    
    [JsonPropertyName("code")]
    public int? Code { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }
}

public class EntityResponse<T> : EntityResponseBase {
    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

public class EntityRequestAction {
    [JsonPropertyName("action")]
    public required string Action { get; set; }
}
