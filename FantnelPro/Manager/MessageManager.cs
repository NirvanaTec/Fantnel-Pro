using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using FantnelPro.Entities;
using FantnelPro.Handler;
using FantnelPro.Utils;
using FantnelPro.Utils.CodeTools;
using Photino.NET;
using Serilog;

namespace FantnelPro.Manager;

public class MessageManager {
    
    public static void HandleMessage(object? sender, string message)
    {
        _ = HandleMessageAsync(sender as PhotinoWindow, message);
    }

    private static async Task HandleMessageAsync(PhotinoWindow? window, string rawMessage)
    {
        if (window == null) {
            return;
        }

        try {
            var request = JsonSerializer.Deserialize<EntityRequestAction>(rawMessage);
            if (request == null) {
                Log.Error("收到错误消息: {0}", rawMessage);
                return;
            }

            Log.Information("收到消息: action:{0}", request.Action);

            _ = Task.Run(() => {
                var response = request.Action switch {
                    "window:drag-start" => WindowHandler.StartDragInit(),
                    "window:drag-move" => WindowHandler.DragMove(request.Data),
                    "fantnel:init" => Code.ToJson(ErrorCode.InitializeWindow),
                    "window:init" => Program.Init(),
                    "window:minimize" => WindowHandler.Minimize(),
                    "window:close" => WindowHandler.Close(),
                    _ => throw new ErrorCodeException(ErrorCode.InvalidMessageType)
                };
                if (response != null) {
                    window.SendWebMessage(response);
                }
            });
        } catch (Exception ex) {
            try {
                var response = OnException(ex);
                var json = JsonSerializer.Serialize(response);
                await window.SendWebMessageAsync(json);
            } catch {
                Log.Error("处理消息时异常2: {0}", ex.Message);
            }
        }
    }

    private static EntityResponse<object> OnException(Exception e)
    {
        EntityResponse<object>? response; // 信息
        var array = new JsonArray(); // 异常追踪
        if (e is ErrorCodeException errorCodeException) {
            response = errorCodeException.GetJson();
        } else {
            response = new EntityResponse<object> {
                Code = -1,
                Msg = Tools.GetMessage(e)
            };
            var stack = GetStackTrace(e);
            if (stack != null) {
                array.Add(stack);
            }
        }

        var index = array.Count;
        var stackTrace = new StackTrace(e, true);
        foreach (var frame in stackTrace.GetFrames()) {
            var stackTraceFrame = new EntityStackTrace(frame);
            if (stackTraceFrame.IsIgnore()) {
                continue;
            }

            if (index++ > 8) {
                break;
            }

            array.Add(stackTraceFrame.ToJsonDocument());
        }

        response.Data = array;
        return response;
    }

    private static object? GetStackTrace(Exception exception)
    {
        switch (exception) {
            case AggregateException aggregateException: {
                var jsonArray = new JsonArray();
                foreach (var innerException in aggregateException.InnerExceptions) {
                    var stackTrace = GetStackTrace(innerException);
                    if (stackTrace != null) {
                        jsonArray.Add(stackTrace);
                    }
                }

                return jsonArray.Count == 0 ? null : jsonArray;
            }
            case ErrorCodeException errorCodeException:
                return errorCodeException.Entity.Data;
            default:
                return null;
        }
    }
}