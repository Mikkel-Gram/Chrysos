using Chrysos.Models;

namespace Chrysos.Services;

/// <summary>Builds program items from library entries, applying the difficulty duration multiplier.</summary>
public class ProgramBuilder
{
    private readonly LibraryService _library;

    public ProgramBuilder(LibraryService library) => _library = library;

    public static int ScaleDuration(int seconds, DifficultyLevel difficulty)
    {
        var scaled = seconds * UserSettings.DurationMultiplier(difficulty);
        var rounded = (int)Math.Round(scaled / 5.0) * 5;
        return Math.Max(10, rounded);
    }

    public ProgramItem FromExercise(Exercise exercise, DifficultyLevel difficulty, int? durationOverride = null) => new()
    {
        Kind = LibraryItemKind.Exercise,
        SourceId = exercise.Id,
        Name = exercise.Name,
        Category = exercise.Category,
        Alternating = exercise.Alternating,
        Steps = new List<ProgramStep>
        {
            new()
            {
                ExerciseId = exercise.Id,
                Name = exercise.Name,
                Description = exercise.Description,
                VideoUrl = exercise.VideoUrl,
                RequiredEquipment = new List<Equipment>(exercise.RequiredEquipment),
                DurationSeconds = durationOverride ?? ScaleDuration(exercise.DefaultDurationSeconds, difficulty)
            }
        }
    };

    public ProgramItem FromCombo(Combo combo, DifficultyLevel difficulty)
    {
        var steps = new List<ProgramStep>();
        foreach (var item in combo.Items)
        {
            var exercise = _library.GetExercise(item.ExerciseId);
            if (exercise is null)
            {
                continue;
            }

            steps.Add(new ProgramStep
            {
                ExerciseId = exercise.Id,
                Name = exercise.Name,
                Description = exercise.Description,
                VideoUrl = exercise.VideoUrl,
                RequiredEquipment = new List<Equipment>(exercise.RequiredEquipment),
                DurationSeconds = ScaleDuration(item.DurationSecondsOverride ?? exercise.DefaultDurationSeconds, difficulty)
            });
        }

        return new ProgramItem
        {
            Kind = LibraryItemKind.Combo,
            SourceId = combo.Id,
            Name = combo.Name,
            Category = _library.ComboCategory(combo),
            Alternating = combo.Alternating,
            Steps = steps
        };
    }
}
