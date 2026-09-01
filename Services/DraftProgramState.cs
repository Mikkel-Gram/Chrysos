using Chrysos.Models;

namespace Chrysos.Services;

/// <summary>
/// Holds the most recently generated (not yet saved) program plus the options it came from,
/// so the generator form and the preview page can live on separate routes. Persisted so a
/// reload of the preview page keeps working.
/// </summary>
public class DraftProgramState
{
    private const string Key = "chrysos.draft";
    private readonly BrowserInterop _storage;
    private bool _loaded;

    public DraftProgramState(BrowserInterop storage) => _storage = storage;

    public GenerationResult? Result { get; private set; }
    public GeneratorOptions? Options { get; private set; }

    /// <summary>True once the draft has been stored in the program library.</summary>
    public bool Saved { get; private set; }

    public async Task EnsureLoadedAsync()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        var snapshot = await _storage.GetAsync<Snapshot>(Key);
        if (snapshot?.Result is not null)
        {
            Result = snapshot.Result;
            Options = snapshot.Options;
            Saved = snapshot.Saved;
        }
    }

    public async Task SetAsync(GenerationResult result, GeneratorOptions options)
    {
        _loaded = true;
        Result = result;
        Options = options.Clone();
        Saved = false;
        await PersistAsync();
    }

    public async Task MarkSavedAsync()
    {
        Saved = true;
        await PersistAsync();
    }

    /// <summary>Persist changes made in place to the draft program (for example a swapped item).</summary>
    public async Task TouchAsync(bool saved)
    {
        Saved = saved;
        await PersistAsync();
    }

    public async Task ClearAsync()
    {
        Result = null;
        Options = null;
        Saved = false;
        await _storage.RemoveAsync(Key);
    }

    private async Task PersistAsync()
    {
        if (Result is null || Options is null)
        {
            return;
        }

        await _storage.SetAsync(Key, new Snapshot(Result, Options, Saved));
    }

    private record Snapshot(GenerationResult Result, GeneratorOptions Options, bool Saved);
}
