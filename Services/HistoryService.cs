using Chrysos.Models;

namespace Chrysos.Services;

/// <summary>Rolling history of performed sessions. Capped, so it is not kept forever.</summary>
public class HistoryService
{
    private const string Key = "chrysos.history";
    public const int MaxEntries = 30;

    private readonly BrowserInterop _storage;
    private bool _loaded;

    public HistoryService(BrowserInterop storage) => _storage = storage;

    public List<HistoryEntry> Entries { get; private set; } = new();

    public event Action? Changed;

    public async Task EnsureLoadedAsync()
    {
        if (_loaded)
        {
            return;
        }

        Entries = await _storage.GetAsync<List<HistoryEntry>>(Key) ?? new List<HistoryEntry>();
        _loaded = true;
    }

    public async Task<HistoryEntry> AddAsync(WorkoutProgram program, bool completed, int activeSeconds)
    {
        await EnsureLoadedAsync();

        var entry = new HistoryEntry
        {
            Program = program.Clone(),
            Completed = completed,
            ActiveSeconds = activeSeconds,
            PerformedUtc = DateTime.UtcNow
        };

        Entries.Insert(0, entry);
        if (Entries.Count > MaxEntries)
        {
            Entries = Entries.Take(MaxEntries).ToList();
        }

        await PersistAsync();
        return entry;
    }

    public async Task MarkSavedAsync(Guid entryId)
    {
        var entry = Entries.FirstOrDefault(e => e.Id == entryId);
        if (entry is null)
        {
            return;
        }

        entry.SavedToLibrary = true;
        await PersistAsync();
    }

    public async Task DeleteAsync(Guid entryId)
    {
        Entries.RemoveAll(e => e.Id == entryId);
        await PersistAsync();
    }

    public async Task ClearAsync()
    {
        Entries.Clear();
        await PersistAsync();
    }

    private async Task PersistAsync()
    {
        await _storage.SetAsync(Key, Entries);
        Changed?.Invoke();
    }
}
