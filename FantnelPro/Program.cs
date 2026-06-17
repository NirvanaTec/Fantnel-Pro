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
using FantnelPro.Utils.ViewLogger;
using Photino.NET;
using Serilog;

namespace FantnelPro;

public class Program {
    private static int _maxRestarts = 4;
    private static Process? _process;

    public static PhotinoWindow? Window;
    private static string[] _args = [];

    private static EntityInfo? _fantnel;

    [STAThread]
    public static void Main(string[] args)
    {
        
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        
        Logger.LogoInit();
        
        UpdateTools.LogNirvana();
        
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
        FantnelInit(); // 获取基础信息
        UpdateTools.CheckUpdate(_args); // 自更新检测
        UpdateTools.CheckUpdate(ConnectTest); // Fant 更新检测
        return null;
    }

    private static void FantnelInit()
    {
        FantnelInitAsync().GetAwaiter().GetResult();
    }

    private static async Task FantnelInitAsync()
    {
        for (var i = 0; i < 3; i++) {
            try {
                var entity = await X19Extensions.Nirvana.Api<EntityInfo>("/api/fantnel/pro.fantnel");
                if (entity != null) {
                    _fantnel = entity;
                    return;
                }
            } catch (Exception e) {
               Log.Warning("Connect Error: {0}", e.Message);
            }
        }
    }

    public static EntityInfo GetFant()
    {
        if (_fantnel == null) {
            Log.Warning("Connect Error!");
            Thread.Sleep(6000);
            Environment.Exit(1);
        }
        return _fantnel;
    }

    private static void ConnectTest()
    {
        SetDescription("正在启动...");
        while (_maxRestarts > 0) {
            if (_process == null || _process.HasExited) {
                _maxRestarts--;
                var port = Tools.GetUnusedPort(23521);
                var arguments = "\"" + Path.Combine(PathUtil.FantPath, "Fantnel.dll") + $"\" --fantnel_port {port} --MainPid {Environment.ProcessId} --default_skin_id nirvana.dark.slate.blue --update_false --update_ui_false --update_static_false --update_static_system_false --update_static_linux_system_false";
                var startInfo = new ProcessStartInfo {
                    FileName = "dotnet",
                    Arguments = arguments,
                };
                Log.Warning("Start: {0} {1}", startInfo.FileName, startInfo.Arguments);
                _process = Process.Start(startInfo);
                Thread.Sleep(1400);
                Connect(port);
            }
        }
    }

    private static void Connect(int port)
    {
        ConnectAsync(port).GetAwaiter().GetResult();
    }


    private static async Task ConnectAsync(int port)
    {
        SetDescription("Connecting...");

        var isError = true;
        var url = "http://localhost:" + port;

        for (var i = 0; i < 8; i++) {
            try {
                Log.Warning("Connect: {0}", url);
                var x19 = new X19Extensions(url);
                var home = await x19.Api<JsonObject>("/api/home");
                var node = home?["versions"];
                var crcSalt = node?.ToString();
                if (crcSalt is { Length: > 4 }) {
                    isError = false;
                    break;
                }
            } catch (Exception e) {
                Log.Warning("Connect Error: {0}", e.Message);
            }

            Thread.Sleep(1000);
        }

        // 失败了
        if (isError) {
            SetDescription("Connect Error!");
        } else {
            Window?.Load(url); 
            // Window?.Load("http://localhost:5173/");
        }
        
    }

    private static void SetDescription(params string[] description)
    {
        try {
            var response = Code.ToJson1(ErrorCode.SetUpdateTitle, description);
            var json = JsonSerializer.Serialize(response);
            Window?.SendWebMessage(json);
        } catch (Exception ex) {
            Log.Error("SetDescription Error: {0}", ex.Message);
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