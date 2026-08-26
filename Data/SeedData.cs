using Chrysos.Models;

namespace Chrysos.Data;

/// <summary>
/// The standard exercise/combo library that ships with the app. Ids are derived deterministically
/// from the name so that combos can reference exercises and so that a "reset to standard"
/// produces stable data.
/// </summary>
public static class SeedData
{
    public static Guid StableId(string key)
    {
        // Deterministic 128 bit FNV-1a style hash, so built-in ids never change between runs.
        unchecked
        {
            ulong h1 = 14695981039346656037UL;
            ulong h2 = 1099511628211UL;
            foreach (char c in key)
            {
                h1 = (h1 ^ c) * 1099511628211UL;
                h2 = (h2 + c) * 14695981039346656037UL;
                h2 ^= h1 >> 7;
            }

            var bytes = new byte[16];
            BitConverter.TryWriteBytes(bytes.AsSpan(0, 8), h1);
            BitConverter.TryWriteBytes(bytes.AsSpan(8, 8), h2);
            return new Guid(bytes);
        }
    }

    private static Exercise Ex(
        string name,
        ExerciseCategory category,
        IntensityLevel intensity,
        MuscleGroup group,
        SpecificMuscle muscle,
        bool alternating,
        int seconds,
        string description,
        params Equipment[] equipment) => new()
        {
            Id = StableId("exercise:" + name),
            Name = name,
            Category = category,
            Intensity = intensity,
            MuscleGroup = group,
            SpecificMuscle = muscle,
            Alternating = alternating,
            DefaultDurationSeconds = seconds,
            Description = description,
            RequiredEquipment = equipment.ToList(),
            IsBuiltIn = true
        };

    public static List<Exercise> Exercises() => new()
    {
        // ---------------- Warm up ----------------
        Ex("March in Place", ExerciseCategory.WarmUp, IntensityLevel.VeryLight, MuscleGroup.FullBody, SpecificMuscle.Cardiovascular, false, 45,
            "March on the spot, lifting the knees to hip height and swinging the arms."),
        Ex("Arm Circles", ExerciseCategory.WarmUp, IntensityLevel.VeryLight, MuscleGroup.UpperBody, SpecificMuscle.Shoulders, false, 30,
            "Big slow circles with straight arms, half the time forwards and half backwards."),
        Ex("Shoulder Rolls", ExerciseCategory.WarmUp, IntensityLevel.VeryLight, MuscleGroup.UpperBody, SpecificMuscle.Shoulders, false, 30,
            "Roll the shoulders up, back and down in a smooth circle."),
        Ex("Torso Twists", ExerciseCategory.WarmUp, IntensityLevel.VeryLight, MuscleGroup.Core, SpecificMuscle.Obliques, false, 30,
            "Feet hip width apart, rotate the upper body left and right with relaxed arms."),
        Ex("Hip Circles", ExerciseCategory.WarmUp, IntensityLevel.VeryLight, MuscleGroup.LowerBody, SpecificMuscle.HipFlexors, false, 30,
            "Hands on hips, draw large circles with the hips in both directions."),
        Ex("Leg Swings", ExerciseCategory.WarmUp, IntensityLevel.VeryLight, MuscleGroup.LowerBody, SpecificMuscle.Hamstrings, true, 30,
            "Hold on to something stable and swing one leg forwards and backwards in a controlled range."),
        Ex("Ankle Rolls", ExerciseCategory.WarmUp, IntensityLevel.VeryLight, MuscleGroup.LowerBody, SpecificMuscle.Calves, true, 20,
            "Lift one foot and rotate the ankle in both directions."),
        Ex("Cat-Cow", ExerciseCategory.WarmUp, IntensityLevel.VeryLight, MuscleGroup.Core, SpecificMuscle.LowerBack, false, 45,
            "On all fours, alternate between arching and rounding the spine with the breath.", Equipment.Mat),
        Ex("Jumping Jacks", ExerciseCategory.WarmUp, IntensityLevel.Light, MuscleGroup.FullBody, SpecificMuscle.Cardiovascular, false, 45,
            "Jump the feet wide while raising the arms overhead, then back together."),
        Ex("Easy High Knees", ExerciseCategory.WarmUp, IntensityLevel.Light, MuscleGroup.LowerBody, SpecificMuscle.HipFlexors, false, 40,
            "Light jog on the spot bringing the knees up to hip height."),
        Ex("Inchworm Walkout", ExerciseCategory.WarmUp, IntensityLevel.Light, MuscleGroup.FullBody, SpecificMuscle.Hamstrings, false, 45,
            "Fold forward, walk the hands out to a plank, then walk them back and stand up.", Equipment.Mat),
        Ex("Bodyweight Good Morning", ExerciseCategory.WarmUp, IntensityLevel.Light, MuscleGroup.LowerBody, SpecificMuscle.Hamstrings, false, 40,
            "Hands behind the head, hinge at the hips with a flat back and return to standing."),

        // ---------------- Strength ----------------
        Ex("Push-Up", ExerciseCategory.Strength, IntensityLevel.Moderate, MuscleGroup.UpperBody, SpecificMuscle.Chest, false, 45,
            "Hands under the shoulders, body in a straight line, lower the chest towards the floor.", Equipment.Mat),
        Ex("Incline Push-Up", ExerciseCategory.Strength, IntensityLevel.Light, MuscleGroup.UpperBody, SpecificMuscle.Chest, false, 45,
            "Push-up with the hands elevated on a chair or bench to reduce the load.", Equipment.Chair),
        Ex("Diamond Push-Up", ExerciseCategory.Strength, IntensityLevel.Hard, MuscleGroup.UpperBody, SpecificMuscle.Triceps, false, 35,
            "Push-up with the hands close together forming a diamond, elbows tight to the body.", Equipment.Mat),
        Ex("Pike Push-Up", ExerciseCategory.Strength, IntensityLevel.Hard, MuscleGroup.UpperBody, SpecificMuscle.Shoulders, false, 35,
            "Hips high in an inverted V, lower the crown of the head towards the floor.", Equipment.Mat),
        Ex("Triceps Dip", ExerciseCategory.Strength, IntensityLevel.Moderate, MuscleGroup.UpperBody, SpecificMuscle.Triceps, false, 40,
            "Hands on the edge of a chair behind you, bend the elbows to lower the hips.", Equipment.Chair),
        Ex("Dead Hang", ExerciseCategory.Strength, IntensityLevel.Light, MuscleGroup.UpperBody, SpecificMuscle.Forearms, false, 30,
            "Hang from the bar with active shoulders and a relaxed lower body.", Equipment.PullUpBar),
        Ex("Pull-Up", ExerciseCategory.Strength, IntensityLevel.VeryHard, MuscleGroup.UpperBody, SpecificMuscle.Lats, false, 30,
            "Hang from the bar and pull the chest towards it, controlled on the way down.", Equipment.PullUpBar),
        Ex("Bodyweight Squat", ExerciseCategory.Strength, IntensityLevel.Light, MuscleGroup.LowerBody, SpecificMuscle.Quadriceps, false, 50,
            "Feet shoulder width, sit back and down keeping the chest tall and heels down."),
        Ex("Jump Squat", ExerciseCategory.Strength, IntensityLevel.Hard, MuscleGroup.LowerBody, SpecificMuscle.Quadriceps, false, 35,
            "Squat down and explode into a jump, landing softly into the next repetition."),
        Ex("Reverse Lunge", ExerciseCategory.Strength, IntensityLevel.Moderate, MuscleGroup.LowerBody, SpecificMuscle.Quadriceps, true, 40,
            "Step one leg back and lower the back knee towards the floor, then drive back up."),
        Ex("Bulgarian Split Squat", ExerciseCategory.Strength, IntensityLevel.Hard, MuscleGroup.LowerBody, SpecificMuscle.Quadriceps, true, 40,
            "Rear foot elevated on a chair, lower straight down on the front leg.", Equipment.Chair),
        Ex("Step-Up", ExerciseCategory.Strength, IntensityLevel.Moderate, MuscleGroup.LowerBody, SpecificMuscle.Glutes, true, 40,
            "Step up on a box or step driving through the heel, control the way down.", Equipment.Step),
        Ex("Glute Bridge", ExerciseCategory.Strength, IntensityLevel.Light, MuscleGroup.LowerBody, SpecificMuscle.Glutes, false, 45,
            "Lying on your back, feet flat, push the hips up and squeeze the glutes at the top.", Equipment.Mat),
        Ex("Single-Leg Glute Bridge", ExerciseCategory.Strength, IntensityLevel.Moderate, MuscleGroup.LowerBody, SpecificMuscle.Glutes, true, 35,
            "Glute bridge with one leg extended, keeping the hips level.", Equipment.Mat),
        Ex("Wall Sit", ExerciseCategory.Strength, IntensityLevel.Moderate, MuscleGroup.LowerBody, SpecificMuscle.Quadriceps, false, 45,
            "Back against the wall, knees at 90 degrees, hold the position.", Equipment.Wall),
        Ex("Calf Raises", ExerciseCategory.Strength, IntensityLevel.Light, MuscleGroup.LowerBody, SpecificMuscle.Calves, false, 45,
            "Rise up on the toes slowly and lower with control."),
        Ex("Plank", ExerciseCategory.Strength, IntensityLevel.Moderate, MuscleGroup.Core, SpecificMuscle.Abs, false, 45,
            "Forearms under the shoulders, body in one line, ribs down and glutes tight.", Equipment.Mat),
        Ex("Side Plank", ExerciseCategory.Strength, IntensityLevel.Moderate, MuscleGroup.Core, SpecificMuscle.Obliques, true, 30,
            "On one forearm, hips stacked and lifted, body in a straight line.", Equipment.Mat),
        Ex("Dead Bug", ExerciseCategory.Strength, IntensityLevel.Light, MuscleGroup.Core, SpecificMuscle.Abs, false, 45,
            "On your back, extend the opposite arm and leg while keeping the lower back down.", Equipment.Mat),
        Ex("Bird Dog", ExerciseCategory.Strength, IntensityLevel.Light, MuscleGroup.Core, SpecificMuscle.LowerBack, false, 45,
            "On all fours, extend the opposite arm and leg and hold briefly.", Equipment.Mat),
        Ex("Bicycle Crunch", ExerciseCategory.Strength, IntensityLevel.Moderate, MuscleGroup.Core, SpecificMuscle.Obliques, false, 45,
            "Bring the opposite elbow and knee together in a slow cycling motion.", Equipment.Mat),
        Ex("Hollow Body Hold", ExerciseCategory.Strength, IntensityLevel.Hard, MuscleGroup.Core, SpecificMuscle.Abs, false, 30,
            "Lower back pressed into the floor, arms and legs extended and lifted.", Equipment.Mat),
        Ex("Superman Hold", ExerciseCategory.Strength, IntensityLevel.Light, MuscleGroup.Core, SpecificMuscle.LowerBack, false, 30,
            "Face down, lift the chest, arms and legs off the floor and hold.", Equipment.Mat),
        Ex("Dumbbell Shoulder Press", ExerciseCategory.Strength, IntensityLevel.Moderate, MuscleGroup.UpperBody, SpecificMuscle.Shoulders, false, 45,
            "Press the dumbbells overhead without flaring the ribs.", Equipment.Dumbbells),
        Ex("Dumbbell Row", ExerciseCategory.Strength, IntensityLevel.Moderate, MuscleGroup.UpperBody, SpecificMuscle.UpperBack, true, 40,
            "Hinge forward with support and row the dumbbell to the hip.", Equipment.Dumbbells),
        Ex("Dumbbell Biceps Curl", ExerciseCategory.Strength, IntensityLevel.Light, MuscleGroup.UpperBody, SpecificMuscle.Biceps, false, 40,
            "Curl both dumbbells with the elbows pinned to the sides.", Equipment.Dumbbells),
        Ex("Goblet Squat", ExerciseCategory.Strength, IntensityLevel.Moderate, MuscleGroup.LowerBody, SpecificMuscle.Quadriceps, false, 45,
            "Hold a weight at the chest and squat deep with an upright torso.", Equipment.Dumbbells),
        Ex("Romanian Deadlift", ExerciseCategory.Strength, IntensityLevel.Moderate, MuscleGroup.LowerBody, SpecificMuscle.Hamstrings, false, 45,
            "Hinge at the hips with soft knees, weights close to the legs, flat back.", Equipment.Dumbbells),
        Ex("Kettlebell Swing", ExerciseCategory.Strength, IntensityLevel.Hard, MuscleGroup.FullBody, SpecificMuscle.Glutes, false, 40,
            "Hinge and snap the hips to swing the kettlebell to chest height.", Equipment.Kettlebell),
        Ex("Band Pull-Apart", ExerciseCategory.Strength, IntensityLevel.Light, MuscleGroup.UpperBody, SpecificMuscle.UpperBack, false, 40,
            "Hold the band at shoulder height and pull it apart, squeezing the shoulder blades.", Equipment.ResistanceBand),
        Ex("Band Row", ExerciseCategory.Strength, IntensityLevel.Moderate, MuscleGroup.UpperBody, SpecificMuscle.UpperBack, false, 45,
            "Anchor the band and row towards the ribs with the elbows close to the body.", Equipment.ResistanceBand),
        Ex("Band Lateral Walk", ExerciseCategory.Strength, IntensityLevel.Moderate, MuscleGroup.LowerBody, SpecificMuscle.Abductors, false, 40,
            "Band around the thighs, half squat position, step sideways keeping tension.", Equipment.ResistanceBand),
        Ex("Ab Wheel Rollout", ExerciseCategory.Strength, IntensityLevel.VeryHard, MuscleGroup.Core, SpecificMuscle.Abs, false, 30,
            "Roll out slowly keeping the hips tucked and the lower back neutral.", Equipment.AbWheel, Equipment.Mat),

        // ---------------- Cardio ----------------
        Ex("High Knees Run", ExerciseCategory.Cardio, IntensityLevel.Hard, MuscleGroup.FullBody, SpecificMuscle.Cardiovascular, false, 40,
            "Fast run on the spot driving the knees above hip height."),
        Ex("Butt Kicks", ExerciseCategory.Cardio, IntensityLevel.Moderate, MuscleGroup.LowerBody, SpecificMuscle.Hamstrings, false, 40,
            "Jog on the spot kicking the heels towards the glutes."),
        Ex("Mountain Climbers", ExerciseCategory.Cardio, IntensityLevel.Hard, MuscleGroup.FullBody, SpecificMuscle.Abs, false, 40,
            "From a plank, drive the knees towards the chest one at a time at pace.", Equipment.Mat),
        Ex("Burpees", ExerciseCategory.Cardio, IntensityLevel.VeryHard, MuscleGroup.FullBody, SpecificMuscle.Cardiovascular, false, 40,
            "Squat, kick back to a plank, jump the feet in and jump up.", Equipment.Mat),
        Ex("Skater Jumps", ExerciseCategory.Cardio, IntensityLevel.Hard, MuscleGroup.LowerBody, SpecificMuscle.Glutes, false, 40,
            "Bound sideways from foot to foot, landing softly on a bent knee."),
        Ex("Plank Jacks", ExerciseCategory.Cardio, IntensityLevel.Hard, MuscleGroup.Core, SpecificMuscle.Abs, false, 35,
            "In a plank, jump the feet wide and back together while keeping the hips still.", Equipment.Mat),
        Ex("Tuck Jumps", ExerciseCategory.Cardio, IntensityLevel.VeryHard, MuscleGroup.LowerBody, SpecificMuscle.Quadriceps, false, 30,
            "Jump and pull both knees towards the chest, landing softly."),
        Ex("Fast Feet", ExerciseCategory.Cardio, IntensityLevel.Moderate, MuscleGroup.LowerBody, SpecificMuscle.Cardiovascular, false, 30,
            "Small quick steps on the balls of the feet in an athletic stance."),
        Ex("Lateral Shuffle", ExerciseCategory.Cardio, IntensityLevel.Moderate, MuscleGroup.LowerBody, SpecificMuscle.Abductors, false, 40,
            "Shuffle a few steps to one side and back in a low athletic position."),
        Ex("Shadow Boxing", ExerciseCategory.Cardio, IntensityLevel.Moderate, MuscleGroup.UpperBody, SpecificMuscle.Shoulders, false, 60,
            "Punch combinations with light footwork and a high guard."),
        Ex("Bear Crawl", ExerciseCategory.Cardio, IntensityLevel.Hard, MuscleGroup.FullBody, SpecificMuscle.Shoulders, false, 40,
            "Knees hovering just off the floor, crawl forwards and backwards.", Equipment.Mat),
        Ex("Sprint in Place", ExerciseCategory.Cardio, IntensityLevel.VeryHard, MuscleGroup.FullBody, SpecificMuscle.Cardiovascular, false, 30,
            "All-out sprint on the spot with strong arm drive."),
        Ex("Jump Rope", ExerciseCategory.Cardio, IntensityLevel.Moderate, MuscleGroup.FullBody, SpecificMuscle.Calves, false, 60,
            "Steady skipping with small bounces on the balls of the feet.", Equipment.JumpRope),

        // ---------------- Stretching ----------------
        Ex("Standing Hamstring Stretch", ExerciseCategory.Stretching, IntensityLevel.VeryLight, MuscleGroup.LowerBody, SpecificMuscle.Hamstrings, true, 30,
            "One heel forward, hinge at the hips until you feel the back of the leg lengthen."),
        Ex("Standing Quad Stretch", ExerciseCategory.Stretching, IntensityLevel.VeryLight, MuscleGroup.LowerBody, SpecificMuscle.Quadriceps, true, 30,
            "Hold the ankle behind you with the knees together and the hips pushed forward."),
        Ex("Calf Stretch at Wall", ExerciseCategory.Stretching, IntensityLevel.VeryLight, MuscleGroup.LowerBody, SpecificMuscle.Calves, true, 30,
            "Hands on the wall, back leg straight and the heel pressed down.", Equipment.Wall),
        Ex("Kneeling Hip Flexor Stretch", ExerciseCategory.Stretching, IntensityLevel.VeryLight, MuscleGroup.LowerBody, SpecificMuscle.HipFlexors, true, 30,
            "Half kneeling, tuck the pelvis and push the hips gently forward.", Equipment.Mat),
        Ex("Pigeon Pose", ExerciseCategory.Stretching, IntensityLevel.Light, MuscleGroup.LowerBody, SpecificMuscle.Glutes, true, 40,
            "Front shin across the mat, back leg long, fold forward over the front leg.", Equipment.Mat),
        Ex("Figure-4 Glute Stretch", ExerciseCategory.Stretching, IntensityLevel.VeryLight, MuscleGroup.LowerBody, SpecificMuscle.Glutes, true, 30,
            "On your back, ankle across the opposite knee, pull the thigh towards you.", Equipment.Mat),
        Ex("Butterfly Stretch", ExerciseCategory.Stretching, IntensityLevel.VeryLight, MuscleGroup.LowerBody, SpecificMuscle.Adductors, false, 40,
            "Soles of the feet together, let the knees drop and hinge forward gently.", Equipment.Mat),
        Ex("Seated Forward Fold", ExerciseCategory.Stretching, IntensityLevel.VeryLight, MuscleGroup.LowerBody, SpecificMuscle.Hamstrings, false, 40,
            "Legs straight in front, hinge forward with a long spine.", Equipment.Mat),
        Ex("Child's Pose", ExerciseCategory.Stretching, IntensityLevel.VeryLight, MuscleGroup.Core, SpecificMuscle.LowerBack, false, 45,
            "Knees wide, hips towards the heels, arms long and forehead down.", Equipment.Mat),
        Ex("Cobra Stretch", ExerciseCategory.Stretching, IntensityLevel.VeryLight, MuscleGroup.Core, SpecificMuscle.Abs, false, 30,
            "Face down, press the chest up with relaxed shoulders and glutes.", Equipment.Mat),
        Ex("Downward Dog", ExerciseCategory.Stretching, IntensityLevel.Light, MuscleGroup.FullBody, SpecificMuscle.Hamstrings, false, 40,
            "Hips high, heels reaching down, long spine through the arms.", Equipment.Mat),
        Ex("Thread the Needle", ExerciseCategory.Stretching, IntensityLevel.VeryLight, MuscleGroup.UpperBody, SpecificMuscle.UpperBack, true, 30,
            "From all fours, slide one arm under the body and rest on the shoulder.", Equipment.Mat),
        Ex("Supine Spinal Twist", ExerciseCategory.Stretching, IntensityLevel.VeryLight, MuscleGroup.Core, SpecificMuscle.LowerBack, true, 30,
            "On your back, drop both knees to one side with the arms wide.", Equipment.Mat),
        Ex("Doorway Chest Stretch", ExerciseCategory.Stretching, IntensityLevel.VeryLight, MuscleGroup.UpperBody, SpecificMuscle.Chest, false, 30,
            "Forearms on a door frame at shoulder height and step gently through.", Equipment.Wall),
        Ex("Overhead Triceps Stretch", ExerciseCategory.Stretching, IntensityLevel.VeryLight, MuscleGroup.UpperBody, SpecificMuscle.Triceps, true, 25,
            "Hand behind the neck, gently press the elbow back with the other hand."),
        Ex("Neck Side Stretch", ExerciseCategory.Stretching, IntensityLevel.VeryLight, MuscleGroup.UpperBody, SpecificMuscle.Neck, true, 25,
            "Tilt the ear towards the shoulder and let the opposite arm hang heavy.")
    };

    private static ComboItem Item(string exerciseName, int? seconds = null) => new()
    {
        ExerciseId = StableId("exercise:" + exerciseName),
        DurationSecondsOverride = seconds
    };

    public static List<Combo> Combos() => new()
    {
        new Combo
        {
            Id = StableId("combo:Single-Leg Lower Body Combo"),
            Name = "Single-Leg Lower Body Combo",
            Description = "All three exercises on one leg before switching sides.",
            IsBuiltIn = true,
            Alternating = true,
            Items = new List<ComboItem> { Item("Reverse Lunge"), Item("Bulgarian Split Squat"), Item("Single-Leg Glute Bridge") }
        },
        new Combo
        {
            Id = StableId("combo:Hip Opener Combo"),
            Name = "Hip Opener Combo",
            Description = "Complete hip mobility sequence for one side, then repeat on the other.",
            IsBuiltIn = true,
            Alternating = true,
            Items = new List<ComboItem> { Item("Kneeling Hip Flexor Stretch"), Item("Pigeon Pose"), Item("Figure-4 Glute Stretch") }
        },
        new Combo
        {
            Id = StableId("combo:Leg Stretch Combo"),
            Name = "Leg Stretch Combo",
            Description = "Hamstring, quad and calf stretch on the same leg before switching.",
            IsBuiltIn = true,
            Alternating = true,
            Items = new List<ComboItem> { Item("Standing Hamstring Stretch"), Item("Standing Quad Stretch"), Item("Calf Stretch at Wall") }
        },
        new Combo
        {
            Id = StableId("combo:Unilateral Upper & Core Combo"),
            Name = "Unilateral Upper & Core Combo",
            Description = "Row and side plank on one side, then the other.",
            IsBuiltIn = true,
            Alternating = true,
            Items = new List<ComboItem> { Item("Dumbbell Row"), Item("Side Plank") }
        },
        new Combo
        {
            Id = StableId("combo:Standing Mobility Combo"),
            Name = "Standing Mobility Combo",
            Description = "Quick joint warm up for one leg, then the other.",
            IsBuiltIn = true,
            Alternating = true,
            Items = new List<ComboItem> { Item("Leg Swings"), Item("Ankle Rolls") }
        },
        new Combo
        {
            Id = StableId("combo:Upper Body Push Circuit"),
            Name = "Upper Body Push Circuit",
            Description = "Three pushing exercises back to back. Not side specific.",
            IsBuiltIn = true,
            Alternating = false,
            Items = new List<ComboItem> { Item("Push-Up"), Item("Pike Push-Up"), Item("Triceps Dip") }
        }
    };
}
