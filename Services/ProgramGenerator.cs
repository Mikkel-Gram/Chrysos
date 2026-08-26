using Chrysos.Models;

namespace Chrysos.Services;

/// <summary>Builds a random program from the library, honouring equipment, difficulty and focus.</summary>
public class ProgramGenerator
{
    private readonly LibraryService _library;
    private readonly ProgramBuilder _builder;

    public ProgramGenerator(LibraryService library, ProgramBuilder builder)
    {
        _library = library;
        _builder = builder;
    }

    private sealed record Candidate(
        LibraryItemKind Kind,
        Guid Id,
        string Name,
        ExerciseCategory Category,
        IntensityLevel Intensity,
        MuscleGroup Group,
        IReadOnlyList<Equipment> Equipment,
        int BaseSeconds,
        bool Alternating,
        int StepCount);

    public GenerationResult Generate(GeneratorOptions options, UserSettings settings, int? seed = null)
    {
        var random = seed.HasValue ? new Random(seed.Value) : new Random();
        var maxIntensity = UserSettings.MaxIntensity(options.Difficulty);
        var owned = settings.OwnedEquipment;
        var restSeconds = settings.RestSeconds;

        var candidates = BuildCandidates(options, owned, maxIntensity);
        var totalSeconds = options.TotalMinutes * 60;
        var rounds = options.Sets == SetsOption.Random ? random.Next(1, 4) : (int)options.Sets;

        var mix = options.CategoryMix
            .Where(kv => kv.Value > 0)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        if (mix.Count == 0)
        {
            mix = new Dictionary<ExerciseCategory, int> { [ExerciseCategory.Strength] = 100 };
        }

        var mixTotal = mix.Values.Sum();
        var program = new WorkoutProgram
        {
            Name = BuildName(options),
            WasGenerated = true,
            RestSeconds = restSeconds,
            GeneratedWith = options.Clone(),
            Description = $"Randomly generated {options.TotalMinutes} minute session, {rounds} {(rounds == 1 ? "set" : "sets")} per work group."
        };

        var used = new HashSet<Guid>();
        var skippedCategories = new List<ExerciseCategory>();
        var picked = new Dictionary<ExerciseCategory, List<ProgramItem>>();

        foreach (var category in OrderedCategories(mix.Keys))
        {
            // Work exercises are repeated for every round, so only pick a round's worth of them.
            var roundsForCategory = IsWorkCategory(category) ? rounds : 1;
            var target = (int)Math.Round(totalSeconds * (mix[category] / (double)mixTotal)) / roundsForCategory;
            var pool = candidates.Where(c => c.Category == category).ToList();
            if (pool.Count == 0)
            {
                skippedCategories.Add(category);
                continue;
            }

            var spent = 0;
            while (spent < target)
            {
                var available = pool.Where(c => !used.Contains(c.Id)).ToList();
                if (available.Count == 0)
                {
                    // Allow reuse rather than leaving a large gap in the plan.
                    available = pool;
                }

                var pick = WeightedPick(available, options.FocusMuscleGroups, random);
                if (pick is null)
                {
                    break;
                }

                var itemSeconds = ProgramBuilder.ScaleDuration(pick.BaseSeconds, options.Difficulty) * (pick.Alternating ? 2 : 1);
                if (pick.Kind == LibraryItemKind.Combo)
                {
                    itemSeconds = ComboSeconds(pick.Id, options.Difficulty);
                }

                var cost = itemSeconds + restSeconds * pick.StepCount * (pick.Alternating ? 2 : 1);

                // Stop when the next item would overshoot the category budget by more than half of itself.
                if (spent > 0 && spent + cost > target + itemSeconds / 2)
                {
                    break;
                }

                var bucket = picked.TryGetValue(category, out var list) ? list : picked[category] = new List<ProgramItem>();
                bucket.Add(pick.Kind == LibraryItemKind.Exercise
                    ? _builder.FromExercise(_library.GetExercise(pick.Id)!, options.Difficulty)
                    : _builder.FromCombo(_library.GetCombo(pick.Id)!, options.Difficulty));

                used.Add(pick.Id);
                spent += cost;

                if (used.Count > 500)
                {
                    break;
                }
            }
        }

        // Warm up first, stretching last, strength and cardio interleaved in the middle.
        var work = Interleave(
            Bucket(picked, ExerciseCategory.Strength),
            Bucket(picked, ExerciseCategory.Cardio));
        AssignGroups(work, rounds, random);

        program.Items.AddRange(Bucket(picked, ExerciseCategory.WarmUp));
        program.Items.AddRange(work);
        program.Items.AddRange(Bucket(picked, ExerciseCategory.Stretching));
        program.NormalizeGroups();

        return new GenerationResult(program, skippedCategories, candidates.Count);
    }

    private static bool IsWorkCategory(ExerciseCategory category)
        => category is ExerciseCategory.Strength or ExerciseCategory.Cardio;

    /// <summary>Chance that the work block is left as one big group instead of being split up.</summary>
    private const double SingleGroupChance = 0.05;

    /// <summary>Splits the work block into circuits of 3-4 exercises, occasionally leaving it as one.</summary>
    private static void AssignGroups(List<ProgramItem> work, int rounds, Random random)
    {
        if (work.Count == 0)
        {
            return;
        }

        // Pick a group count first and split as evenly as possible, so a remainder of one or two
        // exercises is spread over the other groups instead of collapsing everything into one.
        var groupCount = random.NextDouble() < SingleGroupChance
            ? 1
            : Math.Max(1, (int)Math.Round(work.Count / 3.5));

        var baseSize = work.Count / groupCount;
        var extra = work.Count % groupCount;

        var index = 0;
        for (int group = 1; group <= groupCount; group++)
        {
            var size = baseSize + (group <= extra ? 1 : 0);
            for (int i = 0; i < size; i++)
            {
                work[index].GroupIndex = group;
                work[index].Rounds = rounds;
                index++;
            }
        }
    }

    private static List<ProgramItem> Bucket(Dictionary<ExerciseCategory, List<ProgramItem>> picked, ExerciseCategory category)
        => picked.TryGetValue(category, out var list) ? list : new List<ProgramItem>();

    /// <summary>Rolls a single new item to swap in for one entry of an existing program.</summary>
    public ProgramItem? CreateReplacement(WorkoutProgram program, int index, GeneratorOptions options, UserSettings settings, int? seed = null)
    {
        if (index < 0 || index >= program.Items.Count)
        {
            return null;
        }

        var random = seed.HasValue ? new Random(seed.Value) : new Random();
        var current = program.Items[index];
        var category = current.Category ?? ExerciseCategory.Strength;
        var maxIntensity = UserSettings.MaxIntensity(options.Difficulty);

        var pool = BuildCandidates(options, settings.OwnedEquipment, maxIntensity)
            .Where(c => c.Category == category)
            .ToList();

        var inUse = program.Items.Select(i => i.SourceId).ToHashSet();
        var fresh = pool.Where(c => !inUse.Contains(c.Id)).ToList();
        if (fresh.Count == 0)
        {
            // Everything of this category is already in the plan; at least avoid picking the same item again.
            fresh = pool.Where(c => c.Id != current.SourceId).ToList();
        }

        if (fresh.Count == 0)
        {
            return null;
        }

        var pick = WeightedPick(fresh, options.FocusMuscleGroups, random);
        if (pick is null)
        {
            return null;
        }

        var replacement = pick.Kind == LibraryItemKind.Exercise
            ? _builder.FromExercise(_library.GetExercise(pick.Id)!, options.Difficulty)
            : _builder.FromCombo(_library.GetCombo(pick.Id)!, options.Difficulty);

        // Stay in the same group and round count as the item being swapped out.
        replacement.GroupIndex = current.GroupIndex;
        replacement.Rounds = current.Rounds;
        return replacement;
    }

    /// <summary>Spreads two blocks evenly through each other so the middle alternates strength and cardio.</summary>
    private static List<ProgramItem> Interleave(List<ProgramItem> primary, List<ProgramItem> secondary)
    {
        if (primary.Count == 0)
        {
            return secondary;
        }

        if (secondary.Count == 0)
        {
            return primary;
        }

        var merged = new List<(double Position, int Tie, ProgramItem Item)>();
        for (int i = 0; i < primary.Count; i++)
        {
            merged.Add(((i + 0.5) / primary.Count, 0, primary[i]));
        }

        for (int i = 0; i < secondary.Count; i++)
        {
            merged.Add(((i + 0.5) / secondary.Count, 1, secondary[i]));
        }

        return merged.OrderBy(m => m.Position).ThenBy(m => m.Tie).Select(m => m.Item).ToList();
    }

    private int ComboSeconds(Guid comboId, DifficultyLevel difficulty)
    {
        var combo = _library.GetCombo(comboId);
        if (combo is null)
        {
            return 0;
        }

        var perSide = combo.Items.Sum(i =>
        {
            var exercise = _library.GetExercise(i.ExerciseId);
            var baseSeconds = i.DurationSecondsOverride ?? exercise?.DefaultDurationSeconds ?? 0;
            return ProgramBuilder.ScaleDuration(baseSeconds, difficulty);
        });

        return perSide * (combo.Alternating ? 2 : 1);
    }

    private List<Candidate> BuildCandidates(GeneratorOptions options, List<Equipment> owned, IntensityLevel maxIntensity)
    {
        var result = new List<Candidate>();

        foreach (var exercise in _library.Exercises)
        {
            if (exercise.Intensity > maxIntensity)
            {
                continue;
            }

            if (!exercise.RequiredEquipment.All(owned.Contains))
            {
                continue;
            }

            result.Add(new Candidate(
                LibraryItemKind.Exercise,
                exercise.Id,
                exercise.Name,
                exercise.Category,
                exercise.Intensity,
                exercise.MuscleGroup,
                exercise.RequiredEquipment,
                exercise.DefaultDurationSeconds,
                exercise.Alternating,
                1));
        }

        if (!options.IncludeCombos)
        {
            return result;
        }

        foreach (var combo in _library.Combos)
        {
            var members = _library.ComboExercises(combo).ToList();
            if (members.Count == 0)
            {
                continue;
            }

            var equipment = _library.ComboEquipment(combo);
            if (!equipment.All(owned.Contains))
            {
                continue;
            }

            var intensity = _library.ComboIntensity(combo);
            if (intensity > maxIntensity)
            {
                continue;
            }

            result.Add(new Candidate(
                LibraryItemKind.Combo,
                combo.Id,
                combo.Name,
                _library.ComboCategory(combo),
                intensity,
                _library.ComboMuscleGroup(combo),
                equipment,
                _library.ComboBaseSeconds(combo),
                combo.Alternating,
                members.Count));
        }

        return result;
    }

    private static Candidate? WeightedPick(List<Candidate> pool, List<MuscleGroup> focus, Random random)
    {
        if (pool.Count == 0)
        {
            return null;
        }

        var weights = pool.Select(c => Weight(c, focus)).ToList();
        var total = weights.Sum();
        var roll = random.NextDouble() * total;
        double running = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            running += weights[i];
            if (roll <= running)
            {
                return pool[i];
            }
        }

        return pool[^1];
    }

    private static double Weight(Candidate candidate, List<MuscleGroup> focus)
    {
        if (focus.Count == 0)
        {
            return 1.0;
        }

        if (focus.Contains(candidate.Group))
        {
            return 4.0;
        }

        return candidate.Group == MuscleGroup.FullBody ? 1.5 : 0.4;
    }

    private static IEnumerable<ExerciseCategory> OrderedCategories(IEnumerable<ExerciseCategory> categories)
    {
        var order = new[] { ExerciseCategory.WarmUp, ExerciseCategory.Strength, ExerciseCategory.Cardio, ExerciseCategory.Stretching };
        return order.Where(categories.Contains);
    }

    private static string BuildName(GeneratorOptions options)
    {
        var focus = options.FocusMuscleGroups.Count == 0
            ? "Full body"
            : string.Join(" + ", options.FocusMuscleGroups.Select(f => f.Label()));

        return $"{focus} · {options.TotalMinutes} min";
    }
}

public record GenerationResult(WorkoutProgram Program, List<ExerciseCategory> SkippedCategories, int CandidateCount);
