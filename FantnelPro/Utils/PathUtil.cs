using System.Runtime.InteropServices;

namespace FantnelPro.Utils;

public class PathUtil {
    // Directory.GetCurrentDirectory()
    // AppDomain.CurrentDomain.BaseDirectory
    public static readonly string UpdaterPath = Path.Combine(Directory.GetCurrentDirectory(), "fantnel", "updater");

    // 脚本后缀
    private static readonly string ScriptSuffix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".bat" :
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? ".command" : ".sh";

    public static readonly string ScriptPath = Path.Combine(UpdaterPath, "update" + ScriptSuffix);
}