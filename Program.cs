using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using FantnelPro.Handler;
using FantnelPro.Manager;
using FantnelPro.Utils;
using FantnelPro.Utils.CodeTools;
using FantnelPro.Utils.Update;
using Photino.NET;

namespace FantnelPro;

public class Program {
    
    private static int _maxRestarts = 4;
    private static Process? _process;

    public static PhotinoWindow? Window;
    private static string[] _args = [];

    [STAThread]
    public static void Main(string[] args)
    {
        Window = new PhotinoWindow()
            .SetTitle("涅槃科技 | Fantnel - Pro")
            .SetChromeless(true) // 无标题栏
            .Center()
            .SetSize(1200, 750)
            .SetMinSize(900, 600)
            .SetUseOsDefaultSize(false) // 显式关闭尺寸默认值
            .SetUseOsDefaultLocation(false) // 显式关闭位置默认值
            .SetTransparent(true) // 透明窗口
            .RegisterWebMessageReceivedHandler(MessageManager.HandleMessage)
            .SetUseOsDefaultSize(false)
            .RegisterWindowCreatedHandler((_, _) => {
                WindowHandler.SetWindowRoundedCorners(20);
            });

        _args = args;
        ExtractorResource();

        Window.SetIconFile(GetPathByResourceName("favicon.ico"));

        Window.WaitForClose();
    }

    public static string? Init()
    {
        UpdateTools.CheckUpdate(_args).Wait();
        UpdateTools.CheckUpdate(ConnectTest).Wait();
        return null;
    }

    private static void ConnectTest()
    {
        SetDescription("正在启动...");
        while (_maxRestarts > 0) {
            if (_process == null || _process.HasExited) {
                _maxRestarts--;
                var port = Tools.GetUnusedPort(23521);
                var arguments = Path.Combine(Directory.GetCurrentDirectory(), "fantnel", "Fantnel.dll") + $" --fantnel_port {port}";
                var startInfo = new ProcessStartInfo {
                    FileName = "dotnet",
                    Arguments = arguments,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Console.WriteLine("运行中: {0} {1}", startInfo.FileName, startInfo.Arguments);
                _process = Process.Start(startInfo);
                Thread.Sleep(5000);
                Connect(port);
            }
        }
    }

    private static void Connect(int port)
    {
        SetDescription("正在连接中...");
        var url = "http://localhost:" + port;
        for (var i = 0; i < 5; i++) {
            try {
                Console.WriteLine("正在连接测试...");
                var x19 = new X19Extensions(url);
                var version = x19.Api<JsonObject>("/api/version").Result;
                var code = version?["code"];
                var codeInt = code?.GetValue<int?>();
                if (codeInt == 1) {
                    break;
                }
            } catch (Exception e) {
                Console.WriteLine("连接测试失败: {0}", e.Message);
            }
            Thread.Sleep(1000);
        }
        // Window?.Load(url);
        Window?.Load("http://localhost:5173/");
    }

    public static void SetDescription(string description)
    {
        try {
            var response = Code.ToJson1(ErrorCode.SetUpdateTitle, description);
            var json = JsonSerializer.Serialize(response);
            Window?.SendWebMessage(json);
        } catch (Exception ex) {
            Console.WriteLine($"SetDescription 错误：{ex.Message}");
        }
    }

    private static void ExtractorResource()
    {
        Window?.Load(GetPathByResourceName("index.html"));
    }

    private static Stream GetStream(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream("FantnelPro.wwwroot." + resourceName);
        return stream ?? throw new FileNotFoundException($"未找到嵌入资源: {resourceName}");
    }

    private static string GetPathByResourceName(string resourceName)
    {
        var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "fantnel", "pro");
        Directory.CreateDirectory(outputPath);
        outputPath = Path.Combine(outputPath, resourceName);
        using var resourceStream = GetStream(resourceName);
        using var fileStream = File.Create(outputPath);
        resourceStream.CopyTo(fileStream);
        return outputPath;
    }
}