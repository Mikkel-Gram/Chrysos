using Chrysos.Models;

namespace Chrysos.Services;

/// <summary>Holds the program that the session player should run, surviving a page reload.</summary>
public class SessionState
{
    private const string Key = "chrysos.currentSession";
    private readonly BrowserInterop _storage;

    public SessionState(BrowserInterop storage) => _storage = storage;

    public WorkoutProgram? Pending { get; private set; }

    /// <summary>True when the program is not (yet) part of the saved program library.</summary>
    public bool IsUnsaved { get; private set; }

    public async Task StartAsync(WorkoutProgram program, bool isUnsaved)
    {
        Pending = program;
        IsUnsaved = isUnsaved;
        await _storage.SetAsync(Key, new Snapshot(program, isUnsaved));
    }

    public async Task<WorkoutProgram?> RestoreAsync()
    {
        if (Pending is not null)
        {
            return Pending;
        }

        var snapshot = await _storage.GetAsync<Snapshot>(Key);
        if (snapshot?.Program is null)
        {
            return null;
        }

        Pending = snapshot.Program;
        IsUnsaved = snapshot.IsUnsaved;
        return Pending;
    }

    public async Task ClearAsync()
    {
        Pending = null;
        IsUnsaved = false;
        await _storage.RemoveAsync(Key);
    }

    private record Snapshot(WorkoutProgram Program, bool IsUnsaved);
}
