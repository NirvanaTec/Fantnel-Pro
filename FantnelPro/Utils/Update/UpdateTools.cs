using FantnelPro.Entities;
using Serilog;

namespace FantnelPro.Utils.Update;

public static class UpdateTools {

    public static void LogNirvana()
    {
        for (var i = 0; i < 4; i++) {
            Log.Warning("https://npyyds.top/");
        }
    }
    
    // 自更新检测
    public static async Task CheckUpdate(string[] args)
    {
        if (!"1.0.1".Equals(Program.GetFant().UpdateVersions)) {
            Log.Warning("当前版本已被禁用，请前往官网重新下载！");
            LogNirvana();
            Environment.Exit(1);
            return;
        }

        var update = 0; // 0:正常检查 1:不检查 2:已被检查
        if (args.Any(arg => arg == "--update_false")) {
            update = 2;
        }

        // 正常检查
        if (update == 0 && Tools.IsReleaseVersion()) {
            var filePath = Environment.GetCommandLineArgs()[0];
            await new EntityUpdate {
                Mode = "pro." + PathUtil.SystemArch,
                Name = "Fantnel Pro",
                SafeMode = true,
                Command = FileUtil.GenerateStartScript(filePath)
            }.CheckUpdateSingle(filePath);
        }
    }

    public static async Task CheckUpdate(Action action)
    {
        await new EntityUpdate {
            Mode = PathUtil.SystemArch,
            Name = "Fantnel"
        }.CheckUpdate(PathUtil.FantName);

        // 检查主题
        var theme = ConfigUtil.GetConfig("themeValue", "ui.nirvana.dark.slate.blue");
        await new EntityUpdate {
            Mode = theme,
            Name = "Fantnel UI"
        }.CheckUpdate(PathUtil.FantName);

        action.Invoke();
    }
}