namespace Chrysos.Models;

public enum SessionStepKind
{
    GetReady,
    Work,
    Rest,
    Finished
}

/// <summary>A single timed step in a running session (countdown, work interval or rest).</summary>
public class SessionStep
{
    public SessionStepKind Kind { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? VideoUrl { get; set; }
    public Side Side { get; set; } = Side.None;
    public int DurationSeconds { get; set; }

    /// <summary>Index of the owning program item, used for progress display.</summary>
    public int ItemIndex { get; set; }

    public string? ItemName { get; set; }
    public string? NextTitle { get; set; }

    /// <summary>Which block of the program this step belongs to.</summary>
    public ProgramPhase Phase { get; set; } = ProgramPhase.Main;

    /// <summary>Work group this step belongs to (1-based); 0 for warm-up and stretching.</summary>
    public int GroupIndex { get; set; }

    /// <summary>Total number of work groups in the program.</summary>
    public int GroupCount { get; set; }

    /// <summary>Current round of the group, 1-based.</summary>
    public int Round { get; set; } = 1;

    /// <summary>How many rounds this group runs for.</summary>
    public int TotalRounds { get; set; } = 1;

    /// <summary>Video of the upcoming exercise, so a rest can preview what is coming.</summary>
    public string? NextVideoUrl { get; set; }
}
