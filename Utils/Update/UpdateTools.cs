using System.Text.Json.Nodes;

namespace FantnelPro.Utils.Update;

public static class UpdateTools {
    // 检查更新
    public static async Task CheckUpdate(string[] args)
    {
        var update = 0; // 0:正常检查 1:不检查 2:已被检查
        if (args.Any(arg => arg == "--update_false")) {
            update = 2;
        }

        if (update == 0) {
            // 不检查 - 提醒
            // case 1:
            // {
            //     if (PublicProgram.Release)
            //     {
            //         for (var i = 0; i < 4; i++) Log.Warning("当前版本已取消自动更新，建议前往官网重新下载！");
            //
            //         Thread.Sleep(3000);
            //     }
            //
            //     break;
            // }
            // 正常检查
            await CheckUpdate("static.pro", "Fantnel-Pro", true);
        }
    }

    /**
     * 检查更新
     * @param name 名称
     * @param safe 是否安全模式
     */
    private static async Task<int> CheckUpdate(string mode, string name = "Resource", bool safe = false,
        bool failureLog = true, string pathValue = "")
    {
        var jsonObj = await X19Extensions.Nirvana.Api<JsonObject>(
            $"/api/fantnel/update/get?mode={mode}");

        if (jsonObj == null) {
            if (!failureLog) return -1;
            Console.WriteLine("{0}: {1}", name, mode);
            Console.WriteLine("检查更新失败, 建议更新至最新版本!");
            return -1;
        }

        var data = jsonObj["data"];
        if (data == null) {
            if (!failureLog) return -1;
            Console.WriteLine("{0}: {1}", name, mode);
            Console.WriteLine("检查更新失败, 建议更新至最新版本!");
            return -1;
        }

        var array = data.AsArray();
        await ThreadUpdateTools.CheckUpdate(array, name, safe, pathValue);
        return array.Count;
    }

    public static async Task CheckUpdate(Action action)
    {
        var system = Tools.DetectOperatingSystemMode();
        var arch = Tools.DetectArchitectureMode();
        var mode = system + "." + arch;
        await CheckUpdate(mode, "Fantnel", false, true, "fantnel");
        await CheckUpdate("static", "Fantnel", false, true, "fantnel");
        await CheckUpdate("static." + system, "Fantnel", false, true, "fantnel");
        action.Invoke();
    }
}