namespace Chrysos.Models;

/// <summary>
/// A combo is an ordered set of exercises that are performed together, typically doing every
/// exercise for the left side first and then repeating the whole sequence for the right side.
/// </summary>
public class Combo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ComboItem> Items { get; set; } = new();

    /// <summary>When true the whole sequence is repeated for the other side.</summary>
    public bool Alternating { get; set; } = true;

    public string? VideoUrl { get; set; }
    public bool IsBuiltIn { get; set; }

    public Combo Clone() => new()
    {
        Id = Id,
        Name = Name,
        Description = Description,
        Items = Items.Select(i => new ComboItem { ExerciseId = i.ExerciseId, DurationSecondsOverride = i.DurationSecondsOverride }).ToList(),
        Alternating = Alternating,
        VideoUrl = VideoUrl,
        IsBuiltIn = IsBuiltIn
    };
}

public class ComboItem
{
    public Guid ExerciseId { get; set; }

    /// <summary>Optional override of the exercise default duration (seconds, per side).</summary>
    public int? DurationSecondsOverride { get; set; }
}
