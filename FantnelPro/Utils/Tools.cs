using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using FantnelPro.Utils.CodeTools;
using FantnelPro.Utils.Update;
using Serilog;

namespace FantnelPro.Utils;

public static class Tools {
    private static bool _isDebugMode;

    [Conditional("DEBUG")]
    private static void SetDebugMode()
    {
        _isDebugMode = true;
    }

    public static bool IsReleaseVersion()
    {
        return !IsDebugVersion();
    }

    private static bool IsDebugVersion()
    {
        SetDebugMode();
        return _isDebugMode;
    }

    /**
     * 同步计算文件的SHA256哈希值
     * @param filePath 文件路径
     * @return 文件的SHA256哈希值（小写十六进制字符串）
     */
    public static string ComputeSha256(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) {
            throw new ArgumentException("文件路径不能为空", nameof(filePath));
        }
        if (!File.Exists(filePath)) {
            throw new FileNotFoundException($"文件不存在: {filePath}");
        }
        using var sha256 = SHA256.Create();
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var hashBytes = sha256.ComputeHash(fileStream);
        return Convert.ToHexStringLower(hashBytes);
    }

    // 保存Shell脚本
    public static async Task SaveShellScript(string filePath, string content)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            await File.WriteAllTextAsync(filePath, content, Encoding.GetEncoding(936)); // GBK 编码
        } else {
            await File.WriteAllTextAsync(filePath, content);
            // 设置权限
            Log.Warning("设置权限: {0}", filePath);
            FileUtil.SetUnixFilePermissions(filePath);
        }
    }

    /**
     * 检测当前操作系统并返回对应的模式
     * @return win | linux | mac
     */
    public static string DetectOperatingSystemMode()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "win";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "linux";
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "mac" : "win";
    }

    /**
     * 检测当前架构并返回对应的模式
     * @return arm64 | x64
     */
    public static string DetectArchitectureMode()
    {
        return RuntimeInformation.ProcessArchitecture switch {
            Architecture.Arm64 or Architecture.Arm or Architecture.Armv6 => "arm64",
            _ => "x64"
        };
    }

    /**
     * 获取未被占用的端口
     * @param startPort 起始端口号
     * @return 未被占用的端口号，如果所有端口都被占用则返回-1
     */
    public static int GetUnusedPort(int startPort)
    {
        for (var port = startPort; port <= startPort + 1024; port++) {
            if (!IsPortInUse(port)) {
                return port;
            }
        }

        return -1;
    }

    /**
     * 检查指定端口是否正在被使用
     * @param port 要检查的端口号
     * @return 如果端口正在被使用则返回true，否则返回false
     */
    private static bool IsPortInUse(int port)
    {
        // 获取本机的网络属性信息
        var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

        // 获取所有正在监听的TCP端点（包含端口号）
        var tcpEndPoints = ipGlobalProperties.GetActiveTcpListeners();

        // 遍历检查目标端口是否存在
        return tcpEndPoints.Any(endPoint => endPoint.Port == port);
    }

    // 获取异常信息 【简化版】
    public static string GetMessage(Exception exception)
    {
        switch (exception) {
            case AggregateException aggregateException: {
                var message1 = aggregateException.InnerExceptions.Aggregate("",
                    (current, innerException) => current + GetMessage(innerException) + ", ");
                return message1.TrimEnd(',', ' ');
            }
            case ErrorCodeException errorCodeException: {
                var message = errorCodeException.Entity.Msg;
                if (message != null) {
                    return message;
                }

                break;
            }
        }

        return exception.Message;
    }
    

    
}