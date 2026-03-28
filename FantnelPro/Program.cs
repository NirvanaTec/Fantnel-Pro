using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FantnelPro.Entities;
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

    public static EntityInfo? Fantnel;

    [STAThread]
    public static void Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        
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
            }).RegisterWindowClosingHandler((_, _) => {
                _maxRestarts = 0;
                _process?.Kill();
                return false;
            });

        _args = args;
        ExtractorResource();

        Window.SetIconFile(GetPathByResourceName("favicon.ico"));

        Window.WaitForClose();
    }

    public static string? Init()
    {
        FantnelInit().Wait();
        UpdateTools.CheckUpdate(_args).Wait();
        UpdateTools.CheckUpdate(ConnectTest).Wait();
        return null;
    }
    
    private static async Task FantnelInit()
    {
        for (var i = 0; i < 3; i++) {
            try {
                var entity = await X19Extensions.Nirvana.Api<EntityInfo>("/api/fantnel/pro.fantnel");
                if (entity != null) {
                    Fantnel = entity;
                    return;
                }
            } catch (Exception e) {
               Console.WriteLine("连接服务器失败! 错误信息: {0}", e.Message);
            }
        }
    }

    public static EntityInfo GetFant()
    {
        if (Fantnel == null) {
            Console.WriteLine("连接服务器失败!");
            Thread.Sleep(6000);
            Environment.Exit(1);
        }
        return Fantnel;
    }

    private static void ConnectTest()
    {
        SetDescription("正在启动...");
        while (_maxRestarts > 0) {
            if (_process == null || _process.HasExited) {
                _maxRestarts--;
                var port = Tools.GetUnusedPort(23521);
                var arguments = Path.Combine(PathUtil.FantPath, "Fantnel.dll") + $" --fantnel_port {port} --MainPid {Environment.ProcessId} --default_skin_id nirvana.dark.slate.blue --update_false";
                var startInfo = new ProcessStartInfo {
                    FileName = "dotnet",
                    Arguments = arguments,
                };
                Console.WriteLine("运行中: {0} {1}", startInfo.FileName, startInfo.Arguments);
                _process = Process.Start(startInfo);
                Thread.Sleep(5300);
                Connect(port).Wait();
            }
        }
    }

    private static async Task Connect(int port)
    {
        SetDescription("正在连接中...");
        var isError = true;
        var url = "http://localhost:" + port;

        for (var i = 0; i < 8; i++) {
            try {
                Console.WriteLine("正在连接测试...");
                var x19 = new X19Extensions(url);
                var version = await x19.Api<JsonObject>("/api/version");
                var code = version?["code"];
                var codeInt = code?.GetValue<int?>();
                if (codeInt == 1) {
                    isError = false;
                    break;
                }
            } catch (Exception e) {
                Console.WriteLine("连接测试失败: {0}", e.Message);
            }

            Thread.Sleep(1000);
        }

        // 失败了
        if (isError) {
            SetDescription("连接失败！");
            return;
        }

        await ConnectHome(port);
    }

    private static async Task ConnectHome(int port)
    {
        var isError = true;
        var url = "http://localhost:" + port;

        for (var i = 0; i < 8; i++) {
            try {
                Console.WriteLine("正在连接测试Home...");
                var x19 = new X19Extensions(url);
                var home = await x19.Api<JsonObject>("/api/home");
                var code = home?["gameVersion"];
                var gameVersion = code?.GetValue<string?>();
                if (gameVersion is { Length: > 5 }) {
                    isError = false;
                    break;
                }
            } catch (Exception e) {
                Console.WriteLine("连接测试失败Home: {0}", e.Message);
            }

            Thread.Sleep(1000);
        }

        // 失败了
        if (isError) {
            SetDescription("连接失败！");
        }

        Window?.Load(url);
        // Window?.Load("http://localhost:5173/");
    }

    public static void SetDescription(params string[] description)
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
        Directory.CreateDirectory(PathUtil.FantPath);
        var outputPath = Path.Combine(PathUtil.FantPath, "pro");
        Directory.CreateDirectory(outputPath);
        outputPath = Path.Combine(outputPath, resourceName);
        using var resourceStream = GetStream(resourceName);
        using var fileStream = File.Create(outputPath);
        resourceStream.CopyTo(fileStream);
        return outputPath;
    }
}