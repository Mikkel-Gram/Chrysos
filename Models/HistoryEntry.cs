namespace Chrysos.Models;

/// <summary>A finished (or abandoned) session. History is capped and not kept forever.</summary>
public class HistoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime PerformedUtc { get; set; } = DateTime.UtcNow;
    public WorkoutProgram Program { get; set; } = new();
    public bool Completed { get; set; }
    public int ActiveSeconds { get; set; }
    public bool SavedToLibrary { get; set; }
}
