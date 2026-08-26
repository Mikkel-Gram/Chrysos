using Chrysos.Models;

namespace Chrysos.Services;

public class SettingsService
{
    private const string Key = "chrysos.settings";
    private readonly BrowserInterop _storage;
    private bool _loaded;

    public SettingsService(BrowserInterop storage) => _storage = storage;

    public UserSettings Settings { get; private set; } = new();

    public event Action? Changed;

    public async Task EnsureLoadedAsync()
    {
        if (_loaded)
        {
            return;
        }

        var stored = await _storage.GetAsync<UserSettings>(Key);
        if (stored is not null)
        {
            Settings = stored;
        }

        _loaded = true;
    }

    public async Task SaveAsync(UserSettings settings)
    {
        Settings = settings;
        await _storage.SetAsync(Key, settings);
        Changed?.Invoke();
    }

    public bool CanPerform(IEnumerable<Equipment> required)
        => required.All(e => Settings.OwnedEquipment.Contains(e));
}
