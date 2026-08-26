namespace Chrysos.Models;

/// <summary>
/// A workout program. Items are snapshots of library entries so that a saved program keeps
/// working even if the underlying exercise is later edited or deleted.
/// </summary>
public class WorkoutProgram
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ProgramItem> Items { get; set; } = new();
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public bool WasGenerated { get; set; }
    public int RestSeconds { get; set; } = 15;

    /// <summary>The options that were used when this program was generated (null for manual programs).</summary>
    public GeneratorOptions? GeneratedWith { get; set; }

    public int WorkSeconds => Segments().Sum(s => s.TotalSeconds);

    /// <summary>Number of work intervals; a rest/countdown sits before every one except the first.</summary>
    public int WorkStepCount => Segments().Sum(s => s.WorkStepCount);

    public int TotalSeconds => WorkSeconds + Math.Max(0, WorkStepCount - 1) * RestSeconds;

    /// <summary>
    /// The program split into consecutive runs of items that share a phase and a work group.
    /// Warm-up and stretching are always a single run each; the work phase is split into the
    /// groups that are performed as circuits.
    /// </summary>
    public List<ProgramSegment> Segments()
    {
        var segments = new List<ProgramSegment>();
        for (int i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            var last = segments.Count > 0 ? segments[^1] : null;

            if (last is not null && last.Phase == item.Phase && last.GroupIndex == item.GroupIndex)
            {
                last.Items.Add(item);
                continue;
            }

            segments.Add(new ProgramSegment
            {
                Phase = item.Phase,
                GroupIndex = item.GroupIndex,
                Rounds = item.Phase == ProgramPhase.Main ? Math.Max(1, item.Rounds) : 1,
                StartIndex = i,
                Items = { item }
            });
        }

        return segments;
    }

    /// <summary>Number of work groups in the program.</summary>
    public int GroupCount => Segments().Count(s => s.Phase == ProgramPhase.Main);

    /// <summary>
    /// Renumbers work groups so they run 1, 2, 3… in order, clears the group on warm-up and
    /// stretching items and makes every item in a group agree on the round count.
    /// Call after any reorder, insert or removal.
    /// </summary>
    public void NormalizeGroups()
    {
        var group = 0;
        int? previousKey = null;
        var previousPhase = (ProgramPhase?)null;

        foreach (var item in Items)
        {
            if (item.Phase != ProgramPhase.Main)
            {
                item.GroupIndex = 0;
                item.Rounds = 1;
                previousKey = null;
                previousPhase = item.Phase;
                continue;
            }

            if (previousPhase != ProgramPhase.Main || previousKey != item.GroupIndex)
            {
                group++;
            }

            previousKey = item.GroupIndex;
            previousPhase = ProgramPhase.Main;
            item.GroupIndex = group;
        }

        foreach (var segment in Segments().Where(s => s.Phase == ProgramPhase.Main))
        {
            var rounds = Math.Clamp(segment.Items[0].Rounds, 1, 3);
            foreach (var item in segment.Items)
            {
                item.Rounds = rounds;
            }
        }
    }

    public WorkoutProgram Clone(bool newId = false) => new()
    {
        Id = newId ? Guid.NewGuid() : Id,
        Name = Name,
        Description = Description,
        Items = Items.Select(i => i.Clone()).ToList(),
        CreatedUtc = CreatedUtc,
        WasGenerated = WasGenerated,
        RestSeconds = RestSeconds,
        GeneratedWith = GeneratedWith
    };
}

public class ProgramItem
{
    public LibraryItemKind Kind { get; set; }

    /// <summary>Id of the library exercise/combo this item came from.</summary>
    public Guid SourceId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Category of the library entry this item came from (null for programs saved before phases existed).</summary>
    public ExerciseCategory? Category { get; set; }

    /// <summary>Which block of the program this item belongs to.</summary>
    public ProgramPhase Phase => Category switch
    {
        ExerciseCategory.WarmUp => ProgramPhase.WarmUp,
        ExerciseCategory.Stretching => ProgramPhase.Stretching,
        _ => ProgramPhase.Main
    };

    /// <summary>When true the item (single exercise or full combo) is performed for both sides.</summary>
    public bool Alternating { get; set; }

    /// <summary>Work group this item belongs to (1-based); 0 for warm-up and stretching.</summary>
    public int GroupIndex { get; set; }

    /// <summary>How many times the group this item belongs to is repeated as a circuit.</summary>
    public int Rounds { get; set; } = 1;

    public List<ProgramStep> Steps { get; set; } = new();

    public int TotalSeconds => Steps.Sum(s => s.DurationSeconds) * (Alternating ? 2 : 1);

    public ProgramItem Clone() => new()
    {
        Kind = Kind,
        SourceId = SourceId,
        Name = Name,
        Category = Category,
        Alternating = Alternating,
        GroupIndex = GroupIndex,
        Rounds = Rounds,
        Steps = Steps.Select(s => s.Clone()).ToList()
    };
}

/// <summary>A consecutive run of program items that share a phase and a work group.</summary>
public class ProgramSegment
{
    public ProgramPhase Phase { get; set; }
    public int GroupIndex { get; set; }
    public int Rounds { get; set; } = 1;

    /// <summary>Index of the first item of this segment in the program's flat item list.</summary>
    public int StartIndex { get; set; }

    public List<ProgramItem> Items { get; } = new();

    public int RoundSeconds => Items.Sum(i => i.TotalSeconds);

    public int TotalSeconds => RoundSeconds * Rounds;

    public int WorkStepCount => Items.Sum(i => i.Steps.Count * (i.Alternating ? 2 : 1)) * Rounds;
}

/// <summary>One exercise inside a program item, with the resolved duration for this program.</summary>
public class ProgramStep
{
    public Guid ExerciseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public string? VideoUrl { get; set; }

    public ProgramStep Clone() => new()
    {
        ExerciseId = ExerciseId,
        Name = Name,
        Description = Description,
        DurationSeconds = DurationSeconds,
        VideoUrl = VideoUrl
    };
}
