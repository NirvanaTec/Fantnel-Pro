using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json;
using FantnelPro.Entities;
using FantnelPro.Utils.CodeTools;

namespace FantnelPro.Handler;

public partial class WindowHandler {
    private const uint DwmwaWindowCornerPreference = 33;
    private const int DwmwcpRound = 2;

    // Windows DWM API for rounded corners
    [LibraryImport("dwmapi.dll")]
    private static partial void DwmSetWindowAttribute(IntPtr hwnd, uint dwAttribute, ref int pvAttribute, uint cbAttribute);

    private static Point _dragAnchorWindow;
    private static Point _dragAnchorMouse;

    public static string? StartDragInit()
    {
        if (Program.Window == null) {
            return null;
        }
        // 拖拽开始：记录窗口位置
        _dragAnchorWindow = Program.Window.Location;
        _dragAnchorMouse.X = 0;
        _dragAnchorMouse.Y = 0;
        return null;
    }

    public static string? DragMove(JsonElement? jsonElement)
    {
        var entity = GetEntity<EntityDrag>(jsonElement);
        if (entity == null || Program.Window == null) {
            return null;
        }
        // 用 JS 传来的绝对坐标 - 锚点 = 新位置
        // 关键：第一次 mousemove 时记录鼠标锚点
        if (_dragAnchorMouse is { X: 0, Y: 0 })
        {
            _dragAnchorMouse = new Point(entity.Sx, entity.Sy);
            return null;
        }
        // Log.Warning("DragMove: {0}, {1}, {2}", _dragAnchorWindow.X, entity.Sx, _dragAnchorMouse.X);
        Program.Window.MoveTo(
            _dragAnchorWindow.X + (entity.Sx - _dragAnchorMouse.X),
            _dragAnchorWindow.Y + (entity.Sy - _dragAnchorMouse.Y)
        );
        return null;
    }

    private static T? GetEntity<T>(JsonElement? jsonElement)
    {
        return jsonElement == null ? default : jsonElement.Value.Deserialize<T>();
    }

    public static string? Minimize()
    {
        Program.Window?.SetMinimized(true);
        return null;
    }

    public static string? Close()
    {
        Program.Window?.Close();
        return null;
    }

    public static void SetWindowRoundedCorners(int radius)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            var hwnd = Program.Window?.WindowHandle ?? IntPtr.Zero;
            if (hwnd == IntPtr.Zero) {
                throw new ErrorCodeException(ErrorCode.WindowNotInitialized);
            }
            SetWindowRoundedCornersWindows(hwnd, radius);
        }
    }

    private static void SetWindowRoundedCornersWindows(IntPtr hwnd, int radius)
    {
        try {
            if (radius > 0) {
                var cornerPreference = DwmwcpRound;
                DwmSetWindowAttribute(hwnd, DwmwaWindowCornerPreference, ref cornerPreference, sizeof(int));
            }
        } catch {
            // DWM API 调用失败
        }
    }
}