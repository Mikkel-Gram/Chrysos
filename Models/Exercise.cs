namespace Chrysos.Models;

/// <summary>A single exercise in the library. Exercises are always time based, never rep based.</summary>
public class Exercise
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ExerciseCategory Category { get; set; } = ExerciseCategory.Strength;
    public IntensityLevel Intensity { get; set; } = IntensityLevel.Moderate;
    public MuscleGroup MuscleGroup { get; set; } = MuscleGroup.FullBody;
    public SpecificMuscle SpecificMuscle { get; set; } = SpecificMuscle.FullBody;
    public List<Equipment> RequiredEquipment { get; set; } = new();

    /// <summary>True when the exercise must be performed for the left and right side separately.</summary>
    public bool Alternating { get; set; }

    /// <summary>Default work duration in seconds. For alternating exercises this is the duration per side.</summary>
    public int DefaultDurationSeconds { get; set; } = 45;

    /// <summary>Optional video (url or relative path under wwwroot). Empty until videos are recorded.</summary>
    public string? VideoUrl { get; set; }

    /// <summary>True for exercises that ship with the app (used by "reset library to standard").</summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// True when the user has edited this built-in exercise. Customized built-ins are left alone
    /// when a new app version merges an updated standard library into the stored one.
    /// </summary>
    public bool IsCustomized { get; set; }

    public Exercise Clone() => new()
    {
        Id = Id,
        Name = Name,
        Description = Description,
        Category = Category,
        Intensity = Intensity,
        MuscleGroup = MuscleGroup,
        SpecificMuscle = SpecificMuscle,
        RequiredEquipment = new List<Equipment>(RequiredEquipment),
        Alternating = Alternating,
        DefaultDurationSeconds = DefaultDurationSeconds,
        VideoUrl = VideoUrl,
        IsBuiltIn = IsBuiltIn,
        IsCustomized = IsCustomized
    };

    /// <summary>Compares everything a user can edit, so an untouched built-in can be told from an edited one.</summary>
    public bool HasSameContentAs(Exercise other)
        => Name == other.Name
           && Description == other.Description
           && Category == other.Category
           && Intensity == other.Intensity
           && MuscleGroup == other.MuscleGroup
           && SpecificMuscle == other.SpecificMuscle
           && Alternating == other.Alternating
           && DefaultDurationSeconds == other.DefaultDurationSeconds
           && VideoUrl == other.VideoUrl
           && RequiredEquipment.Count == other.RequiredEquipment.Count
           && !RequiredEquipment.Except(other.RequiredEquipment).Any();
}
