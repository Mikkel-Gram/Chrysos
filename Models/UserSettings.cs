namespace Chrysos.Models;

public class UserSettings
{
    public List<Equipment> OwnedEquipment { get; set; } = new() { Equipment.Mat, Equipment.Chair, Equipment.Wall };
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Intermediate;
    public int RestSeconds { get; set; } = 10;
    public int CountdownSeconds { get; set; } = 5;
    public bool SoundEnabled { get; set; } = true;
    public bool KeepScreenAwake { get; set; } = true;

    /// <summary>Duration multiplier applied to the default duration of every exercise.</summary>
    public static double DurationMultiplier(DifficultyLevel level) => level switch
    {
        DifficultyLevel.Beginner => 0.75,
        DifficultyLevel.Advanced => 1.3,
        _ => 1.0
    };

    /// <summary>Highest exercise intensity that may be picked by the random generator.</summary>
    public static IntensityLevel MaxIntensity(DifficultyLevel level) => level switch
    {
        DifficultyLevel.Beginner => IntensityLevel.Moderate,
        DifficultyLevel.Advanced => IntensityLevel.VeryHard,
        _ => IntensityLevel.Hard
    };

    public UserSettings Clone() => new()
    {
        OwnedEquipment = new List<Equipment>(OwnedEquipment),
        Difficulty = Difficulty,
        RestSeconds = RestSeconds,
        CountdownSeconds = CountdownSeconds,
        SoundEnabled = SoundEnabled,
        KeepScreenAwake = KeepScreenAwake
    };
}
