using System.Text.Json;
using FantnelPro.Entities;

namespace FantnelPro.Utils.CodeTools;

public static class Code {
    
    public static EntityResponse<string> ToJson(Exception t)
    {
        var json = new EntityResponse<string> {
            Code = -1,
            Msg = t.Message
        };
        return json;
    }

    public static string ToJson(ErrorCode code, object? data = null)
    {
        return JsonSerializer.Serialize(ToJson1(code, data));
    }
    
    public static EntityResponse<object> ToJson1(ErrorCode code, object? data = null)
    {
        return ToJson1(code, new EntityResponse<object>(), data);
    }

    private static EntityResponse<object> ToJson1(ErrorCode code, EntityResponse<object> json, object? data = null)
    {
        json.Code = (int)code;
        json.Msg = GetMessage(code);
        json.Data = data;
        return json;
    }

    public static string GetMessage(ErrorCode code)
    {
        return code switch {
            ErrorCode.Failure => "失败",
            ErrorCode.Success => "成功",
            ErrorCode.WindowNotInitialized => "窗口未初始化",
            ErrorCode.InvalidMessageType => "无效的消息类型",
            ErrorCode.SetUpdateTitle => "设置标题",
            ErrorCode.InitializeWindow => "初始化窗口",
            _ => "未知错误"
        };
    }
}