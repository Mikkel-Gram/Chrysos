using Chrysos.Models;

namespace Chrysos.Services;

/// <summary>The user's saved program library.</summary>
public class ProgramLibraryService
{
    private const string Key = "chrysos.programs";
    private readonly BrowserInterop _storage;
    private bool _loaded;

    public ProgramLibraryService(BrowserInterop storage) => _storage = storage;

    public List<WorkoutProgram> Programs { get; private set; } = new();

    public event Action? Changed;

    public async Task EnsureLoadedAsync()
    {
        if (_loaded)
        {
            return;
        }

        Programs = await _storage.GetAsync<List<WorkoutProgram>>(Key) ?? new List<WorkoutProgram>();
        _loaded = true;
    }

    public WorkoutProgram? Get(Guid id) => Programs.FirstOrDefault(p => p.Id == id);

    public bool Contains(Guid id) => Programs.Any(p => p.Id == id);

    public async Task SaveAsync(WorkoutProgram program)
    {
        var index = Programs.FindIndex(p => p.Id == program.Id);
        if (index >= 0)
        {
            Programs[index] = program;
        }
        else
        {
            Programs.Add(program);
        }

        await PersistAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        Programs.RemoveAll(p => p.Id == id);
        await PersistAsync();
    }

    private async Task PersistAsync()
    {
        await _storage.SetAsync(Key, Programs);
        Changed?.Invoke();
    }
}
