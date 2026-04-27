using System.Text.Json;
using System.Text.Json.Nodes;

namespace FantnelPro.Utils;

public static class ConfigUtil {
    
    // 获取配置
    private static T GetConfig<T>(string name, string defaultValue = "")
    {
        return GetConfigNode(name, defaultValue).GetValue<T>();
    }
    
    // 获取配置
    public static string GetConfig(string name, string defaultValue = "")
    {
        return GetConfig<string>(name, defaultValue);
    }
    
    // 获取配置
    private static JsonNode GetConfigNode(string name, string defaultValue = "")
    {
        return GetConfig()[name] ?? defaultValue;
    }

    // 获取配置
    private static JsonObject GetConfig()
    {
        var resourcesPath = Path.Combine(PathUtil.ResourcePath, "config.json");
        if (!File.Exists(resourcesPath)) {
            return new JsonObject();
        }
        return JsonSerializer.Deserialize<JsonObject>(File.ReadAllText(resourcesPath)) ?? new JsonObject();
    }

}