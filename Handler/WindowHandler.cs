using System.Runtime.InteropServices;
using FantnelPro.Utils.CodeTools;

namespace FantnelPro.Handler;

public partial class WindowHandler {

    // Windows API
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial void ReleaseCapture();

    [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
    private static partial void SendMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial void SetCapture(IntPtr hwnd);

    private const uint WmNclbuttondown = 0x00A1;
    private static readonly IntPtr Htcaption = new(0x0002);

    // Linux X11 API
    [LibraryImport("libX11.so.6")]
    private static partial IntPtr XOpenDisplay(IntPtr display);

    [LibraryImport("libX11.so.6")]
    private static partial void XCloseDisplay(IntPtr display);

    [LibraryImport("libX11.so.6")]
    private static partial void XSendEvent(IntPtr display, IntPtr window, [MarshalAs(UnmanagedType.Bool)] bool propagate, long eventMask, ref XEvent eventSend);

    [LibraryImport("libX11.so.6", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr XInternAtom(IntPtr display, string atomName, [MarshalAs(UnmanagedType.Bool)] bool onlyIfExists);

    [LibraryImport("libX11.so.6")]
    private static partial void XFlush(IntPtr display);

    [LibraryImport("libX11.so.6")]
    private static partial void XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type, int format, int mode, ref int data, int nElements);

    private const int Button1 = 1;
    private const int ClientMessage = 33;
    private const long SubstructureNotifyMask = 1 << 19;
    private const long SubstructureRedirectMask = 1 << 20;

    [StructLayout(LayoutKind.Sequential)]
    private struct XClientMessageEvent
    {
        public int type;
        public IntPtr serial;
        public int send_event;
        public IntPtr display;
        public IntPtr window;
        public IntPtr message_type;
        public int format;
        public ClientMessageData data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct ClientMessageData
    {
        [FieldOffset(0)] public unsafe fixed byte b[20];
        [FieldOffset(0)] public unsafe fixed short s[10];
        [FieldOffset(0)] public unsafe fixed long l[5];
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct XEvent
    {
        [FieldOffset(0)] public XClientMessageEvent xclient;
    }

    // macOS Cocoa API
    [LibraryImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
    private static partial void objc_msgSend(IntPtr receiver, IntPtr selector);

    [LibraryImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
    private static partial void objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg);

    [LibraryImport("/System/Library/Frameworks/AppKit.framework/AppKit", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr sel_registerName(string name);

    // Windows DWM API for rounded corners
    [LibraryImport("dwmapi.dll")]
    private static partial void DwmSetWindowAttribute(IntPtr hwnd, uint dwAttribute, ref int pvAttribute, uint cbAttribute);

    private const uint DwmwaWindowCornerPreference = 33;
    private const int DwmwcpRound = 2;

    public static object? StartDrag()
    {
        var hwnd = Program.Window?.WindowHandle ?? IntPtr.Zero;
        if (hwnd == IntPtr.Zero) {
            throw new ErrorCodeException(ErrorCode.WindowNotInitialized);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows: 使用 Win32 API 模拟标题栏拖拽
            // 关键：需要在 UI 线程同步执行，并且要在鼠标按下的上下文中
            StartDragWindows(hwnd);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Linux: 使用 X11 的 _NET_WM_MOVERESIZE
            StartDragLinux(hwnd);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS: 使用 Cocoa 的 performWindowDragWithEvent
            StartDragMacOs(hwnd);
        }

        return null;
    }

    private static void StartDragWindows(IntPtr hwnd)
    {
        try
        {
            // 首先设置捕获，确保我们能接收鼠标消息
            SetCapture(hwnd);
            
            // 释放捕获，然后发送非客户区鼠标按下消息来模拟标题栏拖拽
            // 这是 Windows 无边框窗口拖拽的标准做法
            ReleaseCapture();
            SendMessage(hwnd, WmNclbuttondown, Htcaption, IntPtr.Zero);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"拖拽失败: {ex.Message}");
        }
    }

    private static void StartDragLinux(IntPtr hwnd)
    {
        try
        {
            // 尝试使用 X11 的 _NET_WM_MOVERESIZE
            var display = XOpenDisplay(IntPtr.Zero);
            if (display == IntPtr.Zero)
            {
                return;
            }

            try
            {
                var netWmMoveResize = XInternAtom(display, "_NET_WM_MOVERESIZE", false);

                var xevent = new XEvent
                {
                    xclient = new XClientMessageEvent
                    {
                        type = ClientMessage,
                        send_event = 1,
                        display = display,
                        window = hwnd,
                        message_type = netWmMoveResize,
                        format = 32
                    }
                };

                unsafe
                {
                    xevent.xclient.data.l[0] = 0; // x_root
                    xevent.xclient.data.l[1] = 0; // y_root
                    xevent.xclient.data.l[2] = 8; // _NET_WM_MOVERESIZE_MOVE
                    xevent.xclient.data.l[3] = Button1; // button
                    xevent.xclient.data.l[4] = 0; // unused
                }

                XSendEvent(display, hwnd, false, SubstructureNotifyMask | SubstructureRedirectMask, ref xevent);
                XFlush(display);
            }
            finally
            {
                XCloseDisplay(display);
            }
        }
        catch
        {
            // X11 调用失败
        }
    }

    private static void StartDragMacOs(IntPtr hwnd)
    {
        try
        {
            var performDragSelector = sel_registerName("performWindowDragWithEvent:");

            if (hwnd != IntPtr.Zero && performDragSelector != IntPtr.Zero)
            {
                objc_msgSend(hwnd, performDragSelector);
            }
        }
        catch
        {
            // Cocoa 调用失败
        }
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
        var hwnd = Program.Window?.WindowHandle ?? IntPtr.Zero;
        if (hwnd == IntPtr.Zero)
        {
            throw new ErrorCodeException(ErrorCode.WindowNotInitialized);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            SetWindowRoundedCornersWindows(hwnd, radius);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            SetWindowRoundedCornersLinux(hwnd, radius);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            SetWindowRoundedCornersMacOs(hwnd, radius);
        }

    }

    private static void SetWindowRoundedCornersWindows(IntPtr hwnd, int radius)
    {
        try
        {
            if (radius > 0)
            {
                var cornerPreference = DwmwcpRound;
                DwmSetWindowAttribute(hwnd, DwmwaWindowCornerPreference, ref cornerPreference, sizeof(int));
            }
        }
        catch
        {
            // DWM API 调用失败
        }
    }

    private static void SetWindowRoundedCornersLinux(IntPtr hwnd, int radius)
    {
        try
        {
            if (radius <= 0)
            {
                return;
            }

            // 在 Linux 上，窗口圆角由窗口管理器和合成器控制
            // 我们设置 _GTK_FRAME_EXTENTS 属性来提示合成器
            // 这对于 GTK 合成器和 Compton/Picom 等合成器有效
            
            var display = XOpenDisplay(IntPtr.Zero);
            if (display == IntPtr.Zero)
            {
                return;
            }

            try
            {
                // 获取 _GTK_FRAME_EXTENTS 原子
                var gtkFrameExtentsAtom = XInternAtom(display, "_GTK_FRAME_EXTENTS", false);
                if (gtkFrameExtentsAtom == IntPtr.Zero)
                {
                    return;
                }

                // 设置 frame extents 为圆角半径
                // 格式：left, right, top, bottom（单位为像素）
                int[] extents = [radius, radius, radius, radius];
                
                XChangeProperty(display, hwnd, gtkFrameExtentsAtom, 
                    XInternAtom(display, "CARDINAL", false), 
                    32, 0, ref extents[0], 4);
                
                XFlush(display);
            }
            finally
            {
                XCloseDisplay(display);
            }
        }
        catch
        {
            // X11 调用失败
        }
    }

    private static void SetWindowRoundedCornersMacOs(IntPtr hwnd, int radius)
    {
        try
        {
            if (radius > 0)
            {
                var setCornerRadiusSelector = sel_registerName("setCornerRadius:");
                if (hwnd != IntPtr.Zero && setCornerRadiusSelector != IntPtr.Zero)
                {
                    objc_msgSend(hwnd, setCornerRadiusSelector, new IntPtr(radius));
                }
            }
        }
        catch
        {
            // Cocoa 调用失败
        }
    }

}
