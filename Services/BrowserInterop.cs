using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace Chrysos.Services;

/// <summary>Thin wrapper around browser localStorage, plus the small audio/wake-lock helpers.</summary>
public class BrowserInterop : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = false
    };

    public BrowserInterop(IJSRuntime js) => _js = js;

    /// <summary>Prefix for every key this app stores in localStorage.</summary>
    public const string KeyPrefix = "chrysos.";

    /// <summary>Prefix used before the app was renamed to Chrysos.</summary>
    private const string LegacyKeyPrefix = "ge.";

    private async Task<IJSObjectReference> ModuleAsync()
        => _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./js/app.js");

    public async Task<T?> GetAsync<T>(string key)
    {
        var module = await ModuleAsync();
        var json = await module.InvokeAsync<string?>("get", key);

        if (string.IsNullOrWhiteSpace(json) && key.StartsWith(KeyPrefix, StringComparison.Ordinal))
        {
            // Carry over data saved under the old "Golden Exercise" key names.
            var legacyKey = string.Concat(LegacyKeyPrefix, key.AsSpan(KeyPrefix.Length));
            json = await module.InvokeAsync<string?>("get", legacyKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                await module.InvokeVoidAsync("set", key, json);
                await module.InvokeVoidAsync("remove", legacyKey);
            }
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value)
    {
        var module = await ModuleAsync();
        await module.InvokeVoidAsync("set", key, JsonSerializer.Serialize(value, JsonOptions));
    }

    public async Task RemoveAsync(string key)
    {
        var module = await ModuleAsync();
        await module.InvokeVoidAsync("remove", key);
    }

    public async Task BeepAsync(int frequency, int durationMs, double volume = 0.15)
    {
        var module = await ModuleAsync();
        await module.InvokeVoidAsync("beep", frequency, durationMs, volume);
    }

    public async Task RequestWakeLockAsync()
    {
        var module = await ModuleAsync();
        await module.InvokeVoidAsync("requestWakeLock");
    }

    public async Task ReleaseWakeLockAsync()
    {
        var module = await ModuleAsync();
        await module.InvokeVoidAsync("releaseWakeLock");
    }

    public async Task DownloadJsonAsync(string fileName, string content)
    {
        var module = await ModuleAsync();
        await module.InvokeVoidAsync("downloadJson", fileName, content);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // ignored
            }
        }
    }
}
