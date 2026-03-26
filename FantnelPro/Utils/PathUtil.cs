using System.Runtime.InteropServices;

namespace FantnelPro.Utils;

public static class PathUtil {
    // Directory.GetCurrentDirectory()
    // AppDomain.CurrentDomain.BaseDirectory
    public static readonly string FantPath = Path.Combine(Directory.GetCurrentDirectory(), "fantnel");
    
    public static readonly string UpdaterPath = Path.Combine(FantPath, "updater");

    // 脚本后缀
    private static readonly string ScriptSuffix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".bat" :
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? ".command" : ".sh";

    public static readonly string ScriptPath = Path.Combine(UpdaterPath, "update" + ScriptSuffix);
    
    public static readonly string ResourcePath = Path.Combine(FantPath, "resources");
    
}