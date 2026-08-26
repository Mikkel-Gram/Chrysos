using System.ComponentModel.DataAnnotations;

namespace Chrysos.Models;

public enum ExerciseCategory
{
    [Display(Name = "Warm up")] WarmUp = 0,
    [Display(Name = "Strength")] Strength = 1,
    [Display(Name = "Cardio")] Cardio = 2,
    [Display(Name = "Stretching")] Stretching = 3
}

public enum IntensityLevel
{
    [Display(Name = "Very light")] VeryLight = 1,
    [Display(Name = "Light")] Light = 2,
    [Display(Name = "Moderate")] Moderate = 3,
    [Display(Name = "Hard")] Hard = 4,
    [Display(Name = "Very hard")] VeryHard = 5
}

/// <summary>High level muscle group used for filtering and focus selection.</summary>
public enum MuscleGroup
{
    [Display(Name = "Full body")] FullBody = 0,
    [Display(Name = "Upper body")] UpperBody = 1,
    [Display(Name = "Core")] Core = 2,
    [Display(Name = "Lower body")] LowerBody = 3
}

public enum SpecificMuscle
{
    [Display(Name = "Chest")] Chest,
    [Display(Name = "Upper back")] UpperBack,
    [Display(Name = "Lats")] Lats,
    [Display(Name = "Shoulders")] Shoulders,
    [Display(Name = "Biceps")] Biceps,
    [Display(Name = "Triceps")] Triceps,
    [Display(Name = "Forearms")] Forearms,
    [Display(Name = "Neck")] Neck,
    [Display(Name = "Abs")] Abs,
    [Display(Name = "Obliques")] Obliques,
    [Display(Name = "Lower back")] LowerBack,
    [Display(Name = "Hip flexors")] HipFlexors,
    [Display(Name = "Glutes")] Glutes,
    [Display(Name = "Quadriceps")] Quadriceps,
    [Display(Name = "Hamstrings")] Hamstrings,
    [Display(Name = "Adductors")] Adductors,
    [Display(Name = "Abductors")] Abductors,
    [Display(Name = "Calves")] Calves,
    [Display(Name = "Full body")] FullBody,
    [Display(Name = "Cardiovascular")] Cardiovascular
}

public enum Equipment
{
    [Display(Name = "Exercise mat")] Mat,
    [Display(Name = "Dumbbells")] Dumbbells,
    [Display(Name = "Barbell")] Barbell,
    [Display(Name = "Kettlebell")] Kettlebell,
    [Display(Name = "Resistance band")] ResistanceBand,
    [Display(Name = "Pull-up bar")] PullUpBar,
    [Display(Name = "Jump rope")] JumpRope,
    [Display(Name = "Bench")] Bench,
    [Display(Name = "Chair")] Chair,
    [Display(Name = "Free wall")] Wall,
    [Display(Name = "Step / box")] Step,
    [Display(Name = "Stability ball")] StabilityBall,
    [Display(Name = "Medicine ball")] MedicineBall,
    [Display(Name = "Foam roller")] FoamRoller,
    [Display(Name = "Towel")] Towel,
    [Display(Name = "Gliding sliders")] Sliders,
    [Display(Name = "Dip bars")] DipBars,
    [Display(Name = "Ab wheel")] AbWheel
}

public enum DifficultyLevel
{
    [Display(Name = "Beginner")] Beginner = 0,
    [Display(Name = "Intermediate")] Intermediate = 1,
    [Display(Name = "Advanced")] Advanced = 2
}

public enum Side
{
    None = 0,
    Left = 1,
    Right = 2
}

public enum LibraryItemKind
{
    Exercise = 0,
    Combo = 1
}

/// <summary>The three blocks a program runs through: warm up, the main work, then stretching.</summary>
public enum ProgramPhase
{
    [Display(Name = "Warm-up")] WarmUp = 0,
    [Display(Name = "Work")] Main = 1,
    [Display(Name = "Stretching")] Stretching = 2
}

/// <summary>How many rounds each work group is repeated for when generating.</summary>
public enum SetsOption
{
    [Display(Name = "Random (1-3)")] Random = 0,
    [Display(Name = "1 set")] One = 1,
    [Display(Name = "2 sets")] Two = 2,
    [Display(Name = "3 sets")] Three = 3
}
