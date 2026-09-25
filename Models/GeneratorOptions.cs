namespace Chrysos.Models;

public class GeneratorOptions
{
    public int TotalMinutes { get; set; } = 30;
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Intermediate;
    public LengthMultiplierOption LengthMultiplier { get; set; } = LengthMultiplierOption.Pct100;

    /// <summary>Muscle groups that should get extra attention. Empty means balanced.</summary>
    public List<MuscleGroup> FocusMuscleGroups { get; set; } = new();

    /// <summary>Percentage of the total session time per category. Values are normalised when generating.</summary>
    public Dictionary<ExerciseCategory, int> CategoryMix { get; set; } = new()
    {
        [ExerciseCategory.WarmUp] = 15,
        [ExerciseCategory.Strength] = 45,
        [ExerciseCategory.Cardio] = 20,
        [ExerciseCategory.Stretching] = 20
    };

    public bool IncludeCombos { get; set; } = true;

    /// <summary>Temporary equipment override for this generation only. Null means use user settings.</summary>
    public List<Equipment>? EquipmentOverride { get; set; }

    /// <summary>Exercises that must be included in the next generation.</summary>
    public List<Guid> ForcedExerciseIds { get; set; } = new();

    /// <summary>Combos that must be included in the next generation.</summary>
    public List<Guid> ForcedComboIds { get; set; } = new();

    /// <summary>Exercises that must be excluded from the next generation.</summary>
    public List<Guid> ExcludedExerciseIds { get; set; } = new();

    /// <summary>Combos that must be excluded from the next generation.</summary>
    public List<Guid> ExcludedComboIds { get; set; } = new();

    /// <summary>How many rounds each work group is repeated for.</summary>
    public SetsOption Sets { get; set; } = SetsOption.Random;

    public GeneratorOptions Clone() => new()
    {
        TotalMinutes = TotalMinutes,
        Difficulty = Difficulty,
        LengthMultiplier = LengthMultiplier,
        FocusMuscleGroups = new List<MuscleGroup>(FocusMuscleGroups),
        CategoryMix = new Dictionary<ExerciseCategory, int>(CategoryMix),
        IncludeCombos = IncludeCombos,
        Sets = Sets,
        EquipmentOverride = EquipmentOverride is null ? null : new List<Equipment>(EquipmentOverride),
        ForcedExerciseIds = new List<Guid>(ForcedExerciseIds),
        ForcedComboIds = new List<Guid>(ForcedComboIds),
        ExcludedExerciseIds = new List<Guid>(ExcludedExerciseIds),
        ExcludedComboIds = new List<Guid>(ExcludedComboIds)
    };

    public double LengthScale() => (int)LengthMultiplier / 100.0;

    public List<Equipment> EffectiveEquipment(UserSettings settings)
        => EquipmentOverride is null ? new List<Equipment>(settings.OwnedEquipment) : new List<Equipment>(EquipmentOverride);
}
