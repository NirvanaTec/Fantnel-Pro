using System.Text;
using System.Text.Json;

namespace FantnelPro.Utils;

public class X19Extensions(string baseUrl) {
    public static readonly X19Extensions Nirvana = new("http://110.42.70.32:13423");

    private readonly HttpClient _client = new() {
        BaseAddress = new Uri(baseUrl),
        Timeout = TimeSpan.FromSeconds(8) // 8秒超时
    };

    private async Task<HttpResponseMessage> Api(string url, string? body)
    {
        if (body == null) {
            return await _client.GetAsync(url).ConfigureAwait(false);
        }

        return await _client.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json"))
            .ConfigureAwait(false);
    }

    public async Task<T?> Api<T>(string url)
    {
        return await Api<T>(url, null);
    }

    private async Task<T?> Api<T>(string url, object? body)
    {
        return await Api<T>(url, JsonSerializer.Serialize(body));
    }

    private async Task<T?> Api<T>(string url, string? body)
    {
        var response = await ApiRaw(url, body);
        if (response == null) return default;
        if (typeof(T) == typeof(JsonDocument)) {
            return (T)(object)JsonDocument.Parse(response);
        }

        return JsonSerializer.Deserialize<T>(response);
    }

    private async Task<string?> ApiRaw(string url, string? body)
    {
        var response = await Api(url, body);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<TResult?> Api<TBody, TResult>(string url, TBody? body)
    {
        return await Api<TResult>(url, body);
    }
}