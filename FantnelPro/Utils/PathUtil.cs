using System.Runtime.InteropServices;

namespace FantnelPro.Utils;

public static class PathUtil {
    
    // 系统架构 - win.x64
    public static readonly string DetectOperating = Tools.DetectOperatingSystemMode();
    public static readonly string SystemArch = DetectOperating + "." + Tools.DetectArchitectureMode();
    
    // Directory.GetCurrentDirectory()
    // AppDomain.CurrentDomain.BaseDirectory
    public const string FantName = "fantnel";
    private static readonly string FantRunPath = Directory.GetCurrentDirectory();
    public static readonly string FantPath = Path.Combine(FantRunPath, FantName);
    
    public static readonly string UpdaterPath = Path.Combine(FantPath, "updater");
    public static readonly string UpdaterBasePath = FantRunPath;

    // 脚本后缀
    private static readonly string ScriptSuffix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".bat" : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? ".command" : ".sh";
    public static readonly string ScriptPath = Path.Combine(FantPath, "update" + ScriptSuffix);
    
    public static readonly string ResourcePath = Path.Combine(FantPath, "resources");
    
}