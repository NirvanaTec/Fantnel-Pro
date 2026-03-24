using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FantnelPro.Utils.Update;

public static class FileUtil {
    public static void SetUnixFilePermissions(string filePath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        try {
            var processStartInfo = new ProcessStartInfo("chmod", $"755 \"{filePath}\"") { UseShellExecute = false };
            var process = Process.Start(processStartInfo);
            process?.WaitForExit();
        } catch (Exception e) {
            Console.WriteLine("警告：使用 chmod 设置 {0} 权限时出错: {1}", filePath, e.Message);
        }
    }
}