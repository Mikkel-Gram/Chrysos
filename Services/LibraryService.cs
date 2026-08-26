using Chrysos.Data;
using Chrysos.Models;

namespace Chrysos.Services;

/// <summary>Owns the exercise and combo library (built-in + user created) and its persistence.</summary>
public class LibraryService
{
    private const string ExercisesKey = "chrysos.exercises";
    private const string CombosKey = "chrysos.combos";

    private readonly BrowserInterop _storage;
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

        if (exercises is null || exercises.Count == 0)
        {
            exercises = SeedData.Exercises();
            await _storage.SetAsync(ExercisesKey, exercises);
        }

        if (combos is null)
        {
            combos = SeedData.Combos();
            await _storage.SetAsync(CombosKey, combos);
        }

        Exercises = exercises;
        Combos = combos;
        _loaded = true;
    }

    public Exercise? GetExercise(Guid id) => Exercises.FirstOrDefault(e => e.Id == id);

    public Combo? GetCombo(Guid id) => Combos.FirstOrDefault(c => c.Id == id);

    public async Task SaveExerciseAsync(Exercise exercise)
    {
        var index = Exercises.FindIndex(e => e.Id == exercise.Id);
        if (index >= 0)
        {
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
        Exercises.RemoveAll(e => e.Id == id);

        // Drop the exercise from any combo that referenced it, and remove combos left empty.
        foreach (var combo in Combos)
        {
            combo.Items.RemoveAll(i => i.ExerciseId == id);
        }

        Combos.RemoveAll(c => c.Items.Count == 0);

        await PersistExercisesAsync();
        await PersistCombosAsync();
    }

    public async Task SaveComboAsync(Combo combo)
    {
        var index = Combos.FindIndex(c => c.Id == combo.Id);
        if (index >= 0)
        {
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
        Combos.RemoveAll(c => c.Id == id);
        await PersistCombosAsync();
    }

    /// <summary>Restores the standard library. Custom entries are kept unless <paramref name="keepCustom"/> is false.</summary>
    public async Task ResetToStandardAsync(bool keepCustom = true)
    {
        var customExercises = keepCustom ? Exercises.Where(e => !e.IsBuiltIn).ToList() : new List<Exercise>();
        var customCombos = keepCustom ? Combos.Where(c => !c.IsBuiltIn).ToList() : new List<Combo>();

        Exercises = SeedData.Exercises().Concat(customExercises).ToList();
        Combos = SeedData.Combos().Concat(customCombos).ToList();

        await PersistExercisesAsync();
        await PersistCombosAsync();
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
