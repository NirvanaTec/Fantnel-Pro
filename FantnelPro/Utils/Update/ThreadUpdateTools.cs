using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;

namespace FantnelPro.Utils.Update;

public static class ThreadUpdateTools {
    private static readonly HttpClient HttpClient = new();

    /**
     * 检查更新
     * path: Fantnel1.dll,
     * size: 127,
     * url: http://npyyds.to/Fantnel1.dll,
     * sha256: 73f95f9e0ceb205fc1c4dc50c0769729d7087868c2aef1d504cb38c771ec
     */
    public static async Task CheckUpdate(JsonArray jsonArray, string name, string downPath = "")
    {
        var index = 0;

        foreach (var item in jsonArray) {
            // 下载进度

            if (item == null) {
                continue;
            }

            var url = item["url"]?.GetValue<string>();
            var pathValue = item["path"]?.GetValue<string>();
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(pathValue)) {
                continue;
            }

            // 修复路径
            pathValue = pathValue.Replace('\\', Path.DirectorySeparatorChar);

            var resourcesPath = Directory.GetCurrentDirectory();

            if (!string.IsNullOrEmpty(downPath)) {
                resourcesPath = Path.Combine(resourcesPath, downPath);
            }

            resourcesPath = Path.Combine(resourcesPath, pathValue);


            // 硬盘访问速限制 1 秒 / 32次 ≈ 0.015
            Thread.Sleep(15);

            // 检查是否需要更新
            if (!NeedsUpdate(item, resourcesPath)) {
                continue;
            }

            // 请求速限制 1 秒 / 12次 ≈ 0.083
            // 83 - 15 = 68ms
            Thread.Sleep(68);
            await DownloadWithRetryAsync(url, resourcesPath, name, index++, jsonArray.Count);
        }
    }

    public static async Task CheckUpdateSingle(JsonArray jsonArray, string name, string filePath, bool safeMode = false)
    {
        var index = 0;
        var exeName = "";
        var pathList = new List<string>();
        
        foreach (var item in jsonArray) {
            // 下载进度

            if (item == null) {
                continue;
            }

            var url = item["url"]?.GetValue<string>();
            if (string.IsNullOrEmpty(url)) {
                continue;
            }

            // 修复路径
            var resourcesPath1 = safeMode ? Path.Combine(PathUtil.UpdaterPath, name) : filePath;

            // 硬盘访问速限制 1 秒 / 32次 ≈ 0.015
            Thread.Sleep(15);

            // 检查是否需要更新
            if (!NeedsUpdate(item, filePath)) {
                continue;
            }

            // 请求速限制 1 秒 / 12次 ≈ 0.083
            // 83 - 15 = 68ms
            Thread.Sleep(68);
            exeName = safeMode ? Path.Combine(PathUtil.FantPath, name) : filePath;
            
            pathList.Add(resourcesPath1);
            await DownloadWithRetryAsync(url, resourcesPath1, name, index++, jsonArray.Count);
        }
        
        if (safeMode && index > 0) {
            foreach (var path in Directory.GetFiles(PathUtil.UpdaterPath)) {
                if (pathList.Contains(path)) {
                    continue;
                }
                File.Delete(path);
            }
            await SafeRestart(exeName);
        }
    }

    private static async Task SafeRestart(string exeName)
    {
        Console.WriteLine("正在更新核心资源，这会自动重启[1次]，请稍后...");
        var scriptPath = PathUtil.ScriptPath;
        await Tools.SaveShellScript(scriptPath, GenerateUpdateScript(exeName));
        Process.Start(new ProcessStartInfo {
            FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/bash",
            Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "/C \"" + scriptPath + "\"" : scriptPath,
        });
        Environment.Exit(0);
    }
    
    private static string GenerateUpdateScript(string exeName)
    {
        var updateScript = GenerateUpdateScript(PathUtil.UpdaterPath, Directory.GetCurrentDirectory(), Environment.GetCommandLineArgs()[0]);
        updateScript += GenerateStartScript(exeName);
        Console.WriteLine("更新脚本: {0}", updateScript);
        return updateScript;
    }

    private static string GenerateUpdateScript(string tempDir, string targetDir, string fileToDelete)
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
            ? $"timeout /t 1 /nobreak\ndel \"{fileToDelete}\"\nxcopy /e /y /i \"{tempDir}\\*\" \"{targetDir}\"\n" 
            : $"sleep 1\nrm -f \"{fileToDelete}\"\ncp -r \"{tempDir}/.\" \"{targetDir}\"\n";
    }
    
    private static string GenerateStartScript(string exeName)
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
            ? $"start \"\" \"{exeName}\""
            : $"chmod +x \"{exeName}\"\n{exeName}";
    }
    
    
    /// <summary>
    ///     以异步方式下载文件，并在失败时自动重试。
    /// </summary>
    /// <param name="url">要下载的文件的 URL。</param>
    /// <param name="filePath">文件下载后保存的本地路径。</param>
    /// <param name="name">用于日志输出的文件描述性名称。</param>
    /// <param name="index">当前下载项的索引（从0开始）。</param>
    /// <param name="totalCount">总下载项数量。</param>
    private static async Task DownloadWithRetryAsync(
        string url,
        string filePath,
        string name,
        int index,
        int totalCount)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 开始下载: {name} -> {filePath} [{index + 1}/{totalCount}]");

        try {
            // 确保目标目录存在
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }

            // 使用 GetStreamAsync 获取文件流
            using var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

            // 检查 HTTP 响应状态码
            if (!response.IsSuccessStatusCode) {
                throw new HttpRequestException($"HTTP 请求失败，状态码: {(int)response.StatusCode} ({response.StatusCode})");
            }

            // 打开一个写入文件的流
            await using var fileStream =
                new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            // 将响应内容复制到文件流
            await response.Content.CopyToAsync(fileStream);

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 成功下载: {name}");
            Program.SetDescription($"下载{name}: {index + 1} / {totalCount}");
        } catch (Exception ex) {
            var errorMessage = $"[{DateTime.Now:HH:mm:ss}] 下载 '{name}' 失败: {ex.Message}";
            Console.WriteLine(errorMessage);
            Program.SetDescription(errorMessage);
        }
    }

    private static bool NeedsUpdate(JsonNode item, string filePath)
    {
        // 文件是否存在
        if (!File.Exists(filePath)) {
            return true;
        }

        // 检查文件大小
        var size = item["size"];
        if (size != null) {
            var expectedSize = size.GetValue<long>();
            var actualSize = new FileInfo(filePath).Length;
            if (actualSize != expectedSize) {
                return true;
            }
        }

        // 检查SHA256
        var sha256 = item["sha256"];
        if (sha256 == null) {
            return false;
        }

        var expectedHash = sha256.GetValue<string>();
        var actualHash = Tools.ComputeSha256(filePath);
        return !string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
    }
}