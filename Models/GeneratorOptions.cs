namespace Chrysos.Models;

public class GeneratorOptions
{
    public int TotalMinutes { get; set; } = 30;
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Intermediate;

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

    /// <summary>How many rounds each work group is repeated for.</summary>
    public SetsOption Sets { get; set; } = SetsOption.Random;

    public GeneratorOptions Clone() => new()
    {
        TotalMinutes = TotalMinutes,
        Difficulty = Difficulty,
        FocusMuscleGroups = new List<MuscleGroup>(FocusMuscleGroups),
        CategoryMix = new Dictionary<ExerciseCategory, int>(CategoryMix),
        IncludeCombos = IncludeCombos,
        Sets = Sets
    };
}
