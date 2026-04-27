using System.Text.Json.Nodes;
using FantnelPro.Utils;
using FantnelPro.Utils.Update;

namespace FantnelPro.Entities;

public class EntityUpdateFile {
    
    public required int Index;
    private readonly string? _downloadUrl; // 文件下载地址
    private readonly string? _pathValue; // 文件基础路径
    private readonly long? _fileSize; // 文件大小
    private readonly string? _fileSha256; // 文件SHA256
    private static readonly Lock Lock = new ();

    public EntityUpdateFile(JsonNode? item) {
        // 文件下载地址
        var url = item?["url"];
        if (url != null) {
            _downloadUrl = url.GetValue<string>();
        }
        // 文件大小
        var size = item?["size"];
        if (size != null) {
            _fileSize = size.GetValue<long>();
        }
        // 文件SHA256
        var sha256 = item?["sha256"];
        if (sha256 != null) {
            _fileSha256 = sha256.GetValue<string>();
        }
        // 文件基础路径
        var path = item?["path"];
        if (path != null) {
            _pathValue = path.GetValue<string>().Replace('\\', Path.DirectorySeparatorChar);
        }
    }

    /**
    * 检查单个文件更新
    * @param filePath 完整文件路径
     */
    public async Task<int> CheckUpdate(string filePath, string safeSavePath, Action<double>? downloadProgress = null)
    {
        if (string.IsNullOrEmpty(_downloadUrl)) {
            return 2;
        }
        // 硬盘访问速限制 1 秒 / 66次 ≈ 0.015
        lock (Lock) {
            Thread.Sleep(15);
        }
        // 检查是否需要更新
        if (!NeedsUpdate(filePath)) {
            return 0;
        }
        // 请求速限制 1 秒 / 12次 ≈ 0.083
        lock (Lock) {
            // 83 - 15 = 68ms
            Thread.Sleep(68);
        }
        // 下载文件
        var success = await DownloadUtil.DownloadAsync(_downloadUrl, safeSavePath, progressValue => {
            downloadProgress?.Invoke(progressValue);
        });
        return success ? 1 : 4;
    }

    private bool NeedsUpdate(string filePath)
    {
        // 文件不存在
        if (!File.Exists(filePath)) {
            return true;
        }

        // 文件大小不同
        if (_fileSize != null) {
            var actualSize = new FileInfo(filePath).Length;
            if (actualSize != _fileSize) {
                return true;
            }
        }

        // 检查SHA256
        if (_fileSha256 == null) {
            return false;
        }

        var actualHash = Tools.ComputeSha256(filePath);
        return !string.Equals(actualHash, _fileSha256, StringComparison.OrdinalIgnoreCase);
    }

    public string? GetPath(bool safeMode, params string[] basePathList)
    {
        if (string.IsNullOrEmpty(_pathValue)) {
            return null;
        }
        var list = new List<string> {
            safeMode ? PathUtil.UpdaterPath : PathUtil.UpdaterBasePath
        };
        list.AddRange(basePathList);
        list.Add(_pathValue);
        return Path.Combine(list.ToArray());
    }
    
}