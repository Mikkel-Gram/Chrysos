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

    /// <summary>
    /// True when the user has edited this built-in combo. Customized built-ins are left alone
    /// when a new app version merges an updated standard library into the stored one.
    /// </summary>
    public bool IsCustomized { get; set; }

    public Combo Clone() => new()
    {
        Id = Id,
        Name = Name,
        Description = Description,
        Items = Items.Select(i => new ComboItem { ExerciseId = i.ExerciseId, DurationSecondsOverride = i.DurationSecondsOverride }).ToList(),
        Alternating = Alternating,
        VideoUrl = VideoUrl,
        IsBuiltIn = IsBuiltIn,
        IsCustomized = IsCustomized
    };

    /// <summary>Compares everything a user can edit, so an untouched built-in can be told from an edited one.</summary>
    public bool HasSameContentAs(Combo other)
        => Name == other.Name
           && Description == other.Description
           && Alternating == other.Alternating
           && VideoUrl == other.VideoUrl
           && Items.Count == other.Items.Count
           && Items.Zip(other.Items).All(pair =>
               pair.First.ExerciseId == pair.Second.ExerciseId
               && pair.First.DurationSecondsOverride == pair.Second.DurationSecondsOverride);
}

public class ComboItem
{
    public Guid ExerciseId { get; set; }

    /// <summary>Optional override of the exercise default duration (seconds, per side).</summary>
    public int? DurationSecondsOverride { get; set; }
}
