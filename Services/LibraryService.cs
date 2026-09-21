using Chrysos.Data;
using Chrysos.Models;

namespace Chrysos.Services;

/// <summary>Owns the exercise and combo library (built-in + user created) and its persistence.</summary>
public class LibraryService
{
    private const string ExercisesKey = "chrysos.exercises";
    private const string CombosKey = "chrysos.combos";
    private const string StateKey = "chrysos.libraryState";

    private readonly BrowserInterop _storage;
    private LibraryState _state = new();
    private bool _loaded;

    public LibraryService(BrowserInterop storage) => _storage = storage;

    public List<Exercise> Exercises { get; private set; } = new();
    public List<Combo> Combos { get; private set; } = new();

    public event Action? Changed;

    public async Task EnsureLoadedAsync()
    {
        if (_loaded)
        {
            return;
        }

        var exercises = await _storage.GetAsync<List<Exercise>>(ExercisesKey);
        var combos = await _storage.GetAsync<List<Combo>>(CombosKey);
        _state = await _storage.GetAsync<LibraryState>(StateKey) ?? new LibraryState();

        var firstRun = exercises is null || exercises.Count == 0;

        Exercises = firstRun ? SeedData.Exercises() : exercises!;
        Combos = combos ?? SeedData.Combos();
        _loaded = true;

        if (firstRun)
        {
            _state.CustomizationBackfillDone = true;
            await PersistAllAsync();
            return;
        }

        if (MergeStandardLibrary())
        {
            await PersistAllAsync();
        }
    }

    /// <summary>
    /// Folds the standard library that ships with the current app version into the stored one:
    /// new built-ins are added, untouched built-ins are refreshed, and entries the user edited or
    /// deleted are left as they are. Returns true when anything changed.
    /// </summary>
    private bool MergeStandardLibrary()
    {
        var seedExercises = SeedData.Exercises();
        var seedCombos = SeedData.Combos();

        var changed = BackfillCustomizationFlags(seedExercises, seedCombos);
        changed |= MergeExercises(seedExercises);
        changed |= MergeCombos(seedCombos);
        changed |= DropComboItemsWithoutExercise();

        return changed;
    }

    /// <summary>
    /// One-off pass for libraries stored before <see cref="Exercise.IsCustomized"/> existed: anything
    /// that no longer matches the seed must be a user edit, so flag it before the first merge runs.
    /// </summary>
    private bool BackfillCustomizationFlags(List<Exercise> seedExercises, List<Combo> seedCombos)
    {
        if (_state.CustomizationBackfillDone)
        {
            return false;
        }

        foreach (var seed in seedExercises)
        {
            var stored = Exercises.FirstOrDefault(e => e.Id == seed.Id);
            if (stored is not null && !stored.IsCustomized && !stored.HasSameContentAs(seed))
            {
                stored.IsCustomized = true;
            }
        }

        foreach (var seed in seedCombos)
        {
            var stored = Combos.FirstOrDefault(c => c.Id == seed.Id);
            if (stored is not null && !stored.IsCustomized && !stored.HasSameContentAs(seed))
            {
                stored.IsCustomized = true;
            }
        }

        _state.CustomizationBackfillDone = true;
        return true;
    }

    private bool MergeExercises(List<Exercise> seedExercises)
    {
        var changed = false;

        foreach (var seed in seedExercises)
        {
            if (_state.RemovedBuiltInExerciseIds.Contains(seed.Id))
            {
                continue;
            }

            var index = Exercises.FindIndex(e => e.Id == seed.Id);
            if (index < 0)
            {
                Exercises.Add(seed);
                changed = true;
            }
            else if (!Exercises[index].IsCustomized && !Exercises[index].HasSameContentAs(seed))
            {
                Exercises[index] = seed;
                changed = true;
            }
        }

        return changed;
    }

    private bool MergeCombos(List<Combo> seedCombos)
    {
        var changed = false;

        foreach (var seed in seedCombos)
        {
            if (_state.RemovedBuiltInComboIds.Contains(seed.Id))
            {
                continue;
            }

            var index = Combos.FindIndex(c => c.Id == seed.Id);
            if (index < 0)
            {
                Combos.Add(seed);
                changed = true;
            }
            else if (!Combos[index].IsCustomized && !Combos[index].HasSameContentAs(seed))
            {
                Combos[index] = seed;
                changed = true;
            }
        }

        return changed;
    }

    /// <summary>
    /// A merged-in combo can reference an exercise the user deleted, which would otherwise count as
    /// zero seconds during a session. Mirror the cleanup that deleting an exercise does.
    /// </summary>
    private bool DropComboItemsWithoutExercise()
    {
        var known = Exercises.Select(e => e.Id).ToHashSet();
        var changed = false;

        foreach (var combo in Combos)
        {
            if (combo.Items.RemoveAll(i => !known.Contains(i.ExerciseId)) > 0)
            {
                // Without this the next merge would restore the full seed combo and strip it again
                // on every single load.
                combo.IsCustomized = true;
                changed = true;
            }
        }

        foreach (var combo in Combos.Where(c => c.Items.Count == 0 && c.IsBuiltIn))
        {
            RememberRemovedCombo(combo);
        }

        changed |= Combos.RemoveAll(c => c.Items.Count == 0) > 0;
        return changed;
    }

    public Exercise? GetExercise(Guid id) => Exercises.FirstOrDefault(e => e.Id == id);

    public Combo? GetCombo(Guid id) => Combos.FirstOrDefault(c => c.Id == id);

    public async Task SaveExerciseAsync(Exercise exercise)
    {
        var index = Exercises.FindIndex(e => e.Id == exercise.Id);
        if (index >= 0)
        {
            // A built-in the user actually changed must survive future standard library merges.
            exercise.IsCustomized = exercise.IsCustomized || (exercise.IsBuiltIn && DiffersFromSeed(exercise));
            Exercises[index] = exercise;
        }
        else
        {
            Exercises.Add(exercise);
        }

        await PersistExercisesAsync();
    }

    public async Task DeleteExerciseAsync(Guid id)
    {
        var exercise = GetExercise(id);
        if (exercise is not null && exercise.IsBuiltIn && !_state.RemovedBuiltInExerciseIds.Contains(id))
        {
            _state.RemovedBuiltInExerciseIds.Add(id);
        }

        Exercises.RemoveAll(e => e.Id == id);

        // Drop the exercise from any combo that referenced it, and remove combos left empty.
        foreach (var combo in Combos)
        {
            if (combo.Items.RemoveAll(i => i.ExerciseId == id) > 0 && combo.IsBuiltIn)
            {
                combo.IsCustomized = true;
            }
        }

        foreach (var combo in Combos.Where(c => c.Items.Count == 0 && c.IsBuiltIn))
        {
            RememberRemovedCombo(combo);
        }

        Combos.RemoveAll(c => c.Items.Count == 0);

        await PersistExercisesAsync();
        await PersistCombosAsync();
        await PersistStateAsync();
    }

    public async Task SaveComboAsync(Combo combo)
    {
        var index = Combos.FindIndex(c => c.Id == combo.Id);
        if (index >= 0)
        {
            combo.IsCustomized = combo.IsCustomized || (combo.IsBuiltIn && DiffersFromSeed(combo));
            Combos[index] = combo;
        }
        else
        {
            Combos.Add(combo);
        }

        await PersistCombosAsync();
    }

    public async Task DeleteComboAsync(Guid id)
    {
        var combo = GetCombo(id);
        if (combo is not null)
        {
            RememberRemovedCombo(combo);
        }

        Combos.RemoveAll(c => c.Id == id);
        await PersistCombosAsync();
        await PersistStateAsync();
    }

    /// <summary>Restores the standard library. Custom entries are kept unless <paramref name="keepCustom"/> is false.</summary>
    public async Task ResetToStandardAsync(bool keepCustom = true)
    {
        var customExercises = keepCustom ? Exercises.Where(e => !e.IsBuiltIn).ToList() : new List<Exercise>();
        var customCombos = keepCustom ? Combos.Where(c => !c.IsBuiltIn).ToList() : new List<Combo>();

        Exercises = SeedData.Exercises().Concat(customExercises).ToList();
        Combos = SeedData.Combos().Concat(customCombos).ToList();

        // A restore is an explicit "give me the standard library back", so forget the deletions and
        // edits that a merge would otherwise keep honouring.
        _state = new LibraryState { CustomizationBackfillDone = true };

        await PersistExercisesAsync();
        await PersistCombosAsync();
        await PersistStateAsync();
    }

    private void RememberRemovedCombo(Combo combo)
    {
        if (combo.IsBuiltIn && !_state.RemovedBuiltInComboIds.Contains(combo.Id))
        {
            _state.RemovedBuiltInComboIds.Add(combo.Id);
        }
    }

    private static bool DiffersFromSeed(Exercise exercise)
    {
        var seed = SeedData.Exercises().FirstOrDefault(e => e.Id == exercise.Id);
        return seed is null || !exercise.HasSameContentAs(seed);
    }

    private static bool DiffersFromSeed(Combo combo)
    {
        var seed = SeedData.Combos().FirstOrDefault(c => c.Id == combo.Id);
        return seed is null || !combo.HasSameContentAs(seed);
    }

    // ---------- combo helpers (derived from the member exercises) ----------

    public IEnumerable<Exercise> ComboExercises(Combo combo)
        => combo.Items.Select(i => GetExercise(i.ExerciseId)).Where(e => e is not null)!.Cast<Exercise>();

    public List<Equipment> ComboEquipment(Combo combo)
        => ComboExercises(combo).SelectMany(e => e.RequiredEquipment).Distinct().OrderBy(e => e.ToString()).ToList();

    public IntensityLevel ComboIntensity(Combo combo)
    {
        var levels = ComboExercises(combo).Select(e => (int)e.Intensity).ToList();
        return levels.Count == 0 ? IntensityLevel.Light : (IntensityLevel)levels.Max();
    }

    public ExerciseCategory ComboCategory(Combo combo)
    {
        var categories = ComboExercises(combo).Select(e => e.Category).ToList();
        return categories.Count == 0
            ? ExerciseCategory.Strength
            : categories.GroupBy(c => c).OrderByDescending(g => g.Count()).ThenBy(g => (int)g.Key).First().Key;
    }

    public MuscleGroup ComboMuscleGroup(Combo combo)
    {
        var groups = ComboExercises(combo).Select(e => e.MuscleGroup).ToList();
        if (groups.Count == 0)
        {
            return MuscleGroup.FullBody;
        }

        var distinct = groups.Distinct().ToList();
        return distinct.Count == 1 ? distinct[0] : MuscleGroup.FullBody;
    }

    /// <summary>Total work seconds for one pass through the combo (one side), at default durations.</summary>
    public int ComboBaseSeconds(Combo combo)
        => combo.Items.Sum(i => i.DurationSecondsOverride ?? GetExercise(i.ExerciseId)?.DefaultDurationSeconds ?? 0);

    public int ComboTotalSeconds(Combo combo)
        => ComboBaseSeconds(combo) * (combo.Alternating ? 2 : 1);

    private async Task PersistAllAsync()
    {
        await _storage.SetAsync(ExercisesKey, Exercises);
        await _storage.SetAsync(CombosKey, Combos);
        await _storage.SetAsync(StateKey, _state);
    }

    private async Task PersistStateAsync() => await _storage.SetAsync(StateKey, _state);

    private async Task PersistExercisesAsync()
    {
        await _storage.SetAsync(ExercisesKey, Exercises);
        Changed?.Invoke();
    }

    private async Task PersistCombosAsync()
    {
        await _storage.SetAsync(CombosKey, Combos);
        Changed?.Invoke();
    }
}
