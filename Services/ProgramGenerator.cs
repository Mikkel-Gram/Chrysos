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
        SpecificMuscle Specific,
        IReadOnlyList<MuscleGroup> MemberGroups,
        IReadOnlyList<SpecificMuscle> MemberSpecificMuscles,
        IReadOnlyList<Guid> MemberExerciseIds,
        IReadOnlyList<Equipment> Equipment,
        int BaseSeconds,
        bool Alternating,
        int StepCount);

    public GenerationResult Generate(GeneratorOptions options, UserSettings settings, int? seed = null)
    {
        var random = seed.HasValue ? new Random(seed.Value) : new Random();
        var maxIntensity = UserSettings.MaxIntensity(options.Difficulty);
        var owned = options.EffectiveEquipment(settings);
        var lengthMultiplier = options.LengthScale();
        var restSeconds = settings.RestSeconds;
        var notices = new List<string>();

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

        var usedCandidateIds = new HashSet<Guid>();
        var usedExerciseIds = new HashSet<Guid>();
        var selectedWorkCandidates = new List<Candidate>();
        var skippedCategories = new List<ExerciseCategory>();
        var picked = new Dictionary<ExerciseCategory, List<ProgramItem>>();
        var spentByCategory = new Dictionary<ExerciseCategory, int>();
        AddForcedItems(
            options,
            owned,
            maxIntensity,
            lengthMultiplier,
            restSeconds,
            picked,
            spentByCategory,
            usedCandidateIds,
            usedExerciseIds,
            selectedWorkCandidates,
            notices);

        foreach (var category in OrderedCategories(mix.Keys).Where(IsWorkCategory))
        {
            // Work exercises are repeated for every round, so only pick a round's worth of them.
            var roundsForCategory = IsWorkCategory(category) ? rounds : 1;
            var weight = mix.TryGetValue(category, out var value) ? value : 0;
            var target = mixTotal == 0
                ? 0
                : (int)Math.Round(totalSeconds * (weight / (double)mixTotal)) / roundsForCategory;
            var pool = candidates.Where(c => c.Category == category).ToList();
            if (pool.Count == 0 && !picked.ContainsKey(category))
            {
                skippedCategories.Add(category);
                continue;
            }

            var spent = spentByCategory.GetValueOrDefault(category, 0);
            while (spent < target)
            {
                var available = pool.Where(c => IsCandidateAvailable(c, usedCandidateIds, usedExerciseIds)).ToList();
                if (available.Count == 0)
                {
                    break;
                }

                var pick = WeightedPick(available, options.FocusMuscleGroups, random);
                if (pick is null)
                {
                    break;
                }

                var cost = Cost(pick, lengthMultiplier, restSeconds);

                // Stop when the next item would overshoot the category budget by more than half of itself.
                if (spent > 0 && spent + cost > target + WorkSeconds(pick, lengthMultiplier) / 2)
                {
                    break;
                }

                var bucket = picked.TryGetValue(category, out var list) ? list : picked[category] = new List<ProgramItem>();
                bucket.Add(pick.Kind == LibraryItemKind.Exercise
                    ? _builder.FromExercise(_library.GetExercise(pick.Id)!, lengthMultiplier)
                    : _builder.FromCombo(_library.GetCombo(pick.Id)!, lengthMultiplier));

                MarkCandidateUsed(pick, usedCandidateIds, usedExerciseIds);
                selectedWorkCandidates.Add(pick);
                spent += cost;
                spentByCategory[category] = spent;

                if (usedCandidateIds.Count > 500)
                {
                    break;
                }
            }
        }

        var workGroups = selectedWorkCandidates
            .SelectMany(c => c.MemberGroups)
            .ToHashSet();
        var workSpecificMuscles = selectedWorkCandidates
            .SelectMany(c => c.MemberSpecificMuscles)
            .ToHashSet();

        foreach (var category in OrderedCategories(mix.Keys).Where(c => !IsWorkCategory(c)))
        {
            var target = (int)Math.Round(totalSeconds * (mix[category] / (double)mixTotal));
            var pool = candidates.Where(c => c.Category == category).ToList();
            if (pool.Count == 0)
            {
                skippedCategories.Add(category);
                continue;
            }

            var spent = spentByCategory.GetValueOrDefault(category, 0);
            while (spent < target)
            {
                var bucket = picked.TryGetValue(category, out var list) ? list : picked[category] = new List<ProgramItem>();
                var available = pool.Where(c => IsCandidateAvailable(c, usedCandidateIds, usedExerciseIds)).ToList();
                if (available.Count == 0)
                {
                    break;
                }

                var biasedPick = bucket.Count < 2;
                var pick = WeightedPick(
                    available,
                    options.FocusMuscleGroups,
                    random,
                    biasedPick
                        ? c => ContextualBias(c, category, workGroups, workSpecificMuscles)
                        : null);
                if (pick is null)
                {
                    break;
                }

                var cost = Cost(pick, lengthMultiplier, restSeconds);
                if (spent > 0 && spent + cost > target + WorkSeconds(pick, lengthMultiplier) / 2)
                {
                    break;
                }

                bucket.Add(pick.Kind == LibraryItemKind.Exercise
                    ? _builder.FromExercise(_library.GetExercise(pick.Id)!, lengthMultiplier)
                    : _builder.FromCombo(_library.GetCombo(pick.Id)!, lengthMultiplier));

                MarkCandidateUsed(pick, usedCandidateIds, usedExerciseIds);
                spent += cost;
                spentByCategory[category] = spent;
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

        return new GenerationResult(program, skippedCategories, candidates.Count, notices);
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
        var equipment = options.EffectiveEquipment(settings);

        var pool = BuildCandidates(options, equipment, maxIntensity)
            .Where(c => c.Category == category)
            .ToList();

        var inUseSourceIds = program.Items
            .Where((_, i) => i != index)
            .Select(i => i.SourceId)
            .ToHashSet();
        var inUseExerciseIds = program.Items
            .Where((_, i) => i != index)
            .SelectMany(i => i.Steps)
            .Select(s => s.ExerciseId)
            .ToHashSet();

        var fresh = pool.Where(c =>
            c.Id != current.SourceId
            && !inUseSourceIds.Contains(c.Id)
            && !c.MemberExerciseIds.Any(inUseExerciseIds.Contains))
            .ToList();

        if (fresh.Count == 0)
        {
            return null;
        }

        var pick = WeightedPick(fresh, options.FocusMuscleGroups, random);
        if (pick is null)
        {
            return null;
        }

        var lengthMultiplier = options.LengthScale();
        var replacement = pick.Kind == LibraryItemKind.Exercise
            ? _builder.FromExercise(_library.GetExercise(pick.Id)!, lengthMultiplier)
            : _builder.FromCombo(_library.GetCombo(pick.Id)!, lengthMultiplier);

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

    private int ComboSeconds(Guid comboId, double lengthMultiplier)
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
            return ProgramBuilder.ScaleDuration(baseSeconds, lengthMultiplier);
        });

        return perSide * (combo.Alternating ? 2 : 1);
    }

    private List<Candidate> BuildCandidates(GeneratorOptions options, List<Equipment> owned, IntensityLevel maxIntensity)
    {
        var result = new List<Candidate>();
        var excludedExercises = options.ExcludedExerciseIds.ToHashSet();
        var excludedCombos = options.ExcludedComboIds.ToHashSet();

        foreach (var exercise in _library.Exercises)
        {
            if (excludedExercises.Contains(exercise.Id))
            {
                continue;
            }

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
                exercise.SpecificMuscle,
                new[] { exercise.MuscleGroup },
                new[] { exercise.SpecificMuscle },
                new[] { exercise.Id },
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
            if (excludedCombos.Contains(combo.Id))
            {
                continue;
            }

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
                members
                    .GroupBy(m => m.SpecificMuscle)
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => (int)g.Key)
                    .First()
                    .Key,
                members.Select(m => m.MuscleGroup).Distinct().ToList(),
                members.Select(m => m.SpecificMuscle).Distinct().ToList(),
                members.Select(m => m.Id).ToList(),
                equipment,
                _library.ComboBaseSeconds(combo),
                combo.Alternating,
                members.Count));
        }

        return result;
    }

    private void AddForcedItems(
        GeneratorOptions options,
        List<Equipment> owned,
        IntensityLevel maxIntensity,
        double lengthMultiplier,
        int restSeconds,
        Dictionary<ExerciseCategory, List<ProgramItem>> picked,
        Dictionary<ExerciseCategory, int> spentByCategory,
        HashSet<Guid> usedCandidateIds,
        HashSet<Guid> usedExerciseIds,
        List<Candidate> selectedWorkCandidates,
        List<string> notices)
    {
        var forcedExercises = options.ForcedExerciseIds.Distinct().ToList();
        var forcedCombos = options.ForcedComboIds.Distinct().ToList();
        var forcedItemCount = 0;

        foreach (var id in forcedExercises)
        {
            var exercise = _library.GetExercise(id);
            if (exercise is null)
            {
                notices.Add("A forced exercise no longer exists and was skipped.");
                continue;
            }

            if (options.ExcludedExerciseIds.Contains(id))
            {
                notices.Add($"'{exercise.Name}' was both forced and excluded. Forced include took priority.");
            }

            if (!exercise.RequiredEquipment.All(owned.Contains))
            {
                notices.Add($"'{exercise.Name}' requires equipment outside your current selection and was still included.");
            }

            if (exercise.Intensity > maxIntensity)
            {
                notices.Add($"'{exercise.Name}' is above your difficulty intensity cap and was still included.");
            }

            AddForcedCandidate(
                new Candidate(
                    LibraryItemKind.Exercise,
                    exercise.Id,
                    exercise.Name,
                    exercise.Category,
                    exercise.Intensity,
                    exercise.MuscleGroup,
                    exercise.SpecificMuscle,
                    new[] { exercise.MuscleGroup },
                    new[] { exercise.SpecificMuscle },
                    new[] { exercise.Id },
                    exercise.RequiredEquipment,
                    exercise.DefaultDurationSeconds,
                    exercise.Alternating,
                    1),
                _builder.FromExercise(exercise, lengthMultiplier));
            forcedItemCount++;
        }

        foreach (var id in forcedCombos)
        {
            var combo = _library.GetCombo(id);
            if (combo is null)
            {
                notices.Add("A forced combo no longer exists and was skipped.");
                continue;
            }

            var equipment = _library.ComboEquipment(combo);
            var intensity = _library.ComboIntensity(combo);
            var category = _library.ComboCategory(combo);
            var group = _library.ComboMuscleGroup(combo);
            var members = _library.ComboExercises(combo).ToList();
            var stepCount = members.Count;
            if (stepCount == 0)
            {
                notices.Add($"'{combo.Name}' has no valid exercises and was skipped.");
                continue;
            }

            if (!options.IncludeCombos)
            {
                notices.Add($"'{combo.Name}' was forced and included even though combos are turned off.");
            }

            if (options.ExcludedComboIds.Contains(id))
            {
                notices.Add($"'{combo.Name}' was both forced and excluded. Forced include took priority.");
            }

            if (!equipment.All(owned.Contains))
            {
                notices.Add($"'{combo.Name}' requires equipment outside your current selection and was still included.");
            }

            if (intensity > maxIntensity)
            {
                notices.Add($"'{combo.Name}' is above your difficulty intensity cap and was still included.");
            }

            AddForcedCandidate(
                new Candidate(
                    LibraryItemKind.Combo,
                    combo.Id,
                    combo.Name,
                    category,
                    intensity,
                    group,
                    members
                        .GroupBy(m => m.SpecificMuscle)
                        .OrderByDescending(g => g.Count())
                        .ThenBy(g => (int)g.Key)
                        .First()
                        .Key,
                    members.Select(m => m.MuscleGroup).Distinct().ToList(),
                    members.Select(m => m.SpecificMuscle).Distinct().ToList(),
                    members.Select(m => m.Id).ToList(),
                    equipment,
                    _library.ComboBaseSeconds(combo),
                    combo.Alternating,
                    stepCount),
                _builder.FromCombo(combo, lengthMultiplier));
            forcedItemCount++;
        }

        if (forcedItemCount > 0)
        {
            notices.Add($"{forcedItemCount} forced item(s) were locked in first, then the rest of the session was auto-adjusted around them.");
        }

        return;

        void AddForcedCandidate(Candidate candidate, ProgramItem item)
        {
            var bucket = picked.TryGetValue(candidate.Category, out var list) ? list : picked[candidate.Category] = new List<ProgramItem>();
            if (usedCandidateIds.Contains(candidate.Id))
            {
                return;
            }

            bucket.Add(item);
            MarkCandidateUsed(candidate, usedCandidateIds, usedExerciseIds);
            if (IsWorkCategory(candidate.Category))
            {
                selectedWorkCandidates.Add(candidate);
            }
            spentByCategory[candidate.Category] = spentByCategory.GetValueOrDefault(candidate.Category, 0) + Cost(candidate, lengthMultiplier, restSeconds);
        }
    }

    private int Cost(Candidate candidate, double lengthMultiplier, int restSeconds)
        => WorkSeconds(candidate, lengthMultiplier) + restSeconds * candidate.StepCount * (candidate.Alternating ? 2 : 1);

    private int WorkSeconds(Candidate candidate, double lengthMultiplier)
        => candidate.Kind == LibraryItemKind.Combo
            ? ComboSeconds(candidate.Id, lengthMultiplier)
            : ProgramBuilder.ScaleDuration(candidate.BaseSeconds, lengthMultiplier) * (candidate.Alternating ? 2 : 1);

    private static bool IsCandidateAvailable(Candidate candidate, HashSet<Guid> usedCandidateIds, HashSet<Guid> usedExerciseIds)
        => !usedCandidateIds.Contains(candidate.Id) && !candidate.MemberExerciseIds.Any(usedExerciseIds.Contains);

    private static void MarkCandidateUsed(Candidate candidate, HashSet<Guid> usedCandidateIds, HashSet<Guid> usedExerciseIds)
    {
        usedCandidateIds.Add(candidate.Id);
        foreach (var exerciseId in candidate.MemberExerciseIds)
        {
            usedExerciseIds.Add(exerciseId);
        }
    }

    private static double ContextualBias(
        Candidate candidate,
        ExerciseCategory category,
        HashSet<MuscleGroup> workGroups,
        HashSet<SpecificMuscle> workSpecificMuscles)
    {
        if (category == ExerciseCategory.WarmUp)
        {
            return candidate.MemberGroups.Any(workGroups.Contains) ? 1.8 : 1.0;
        }

        if (category == ExerciseCategory.Stretching)
        {
            var multiplier = 1.0;
            if (candidate.MemberGroups.Any(workGroups.Contains))
            {
                multiplier *= 1.4;
            }

            if (candidate.MemberSpecificMuscles.Any(workSpecificMuscles.Contains))
            {
                multiplier *= 1.8;
            }

            return multiplier;
        }

        return 1.0;
    }

    private static Candidate? WeightedPick(
        List<Candidate> pool,
        List<MuscleGroup> focus,
        Random random,
        Func<Candidate, double>? contextualBias = null)
    {
        if (pool.Count == 0)
        {
            return null;
        }

        var weights = pool.Select(c => Weight(c, focus) * (contextualBias?.Invoke(c) ?? 1.0)).ToList();
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

public record GenerationResult(WorkoutProgram Program, List<ExerciseCategory> SkippedCategories, int CandidateCount, List<string>? Notices = null);
