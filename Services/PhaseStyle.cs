using Chrysos.Models;

namespace Chrysos.Services;

/// <summary>Maps program phases and work groups onto the shared CSS color classes.</summary>
public static class PhaseStyle
{
    /// <summary>How many per-group work shades are defined in app.css.</summary>
    public const int ShadeCount = 5;

    public static string PhaseClass(ProgramPhase phase) => phase switch
    {
        ProgramPhase.WarmUp => "p-warmup",
        ProgramPhase.Main => "p-main",
        _ => "p-stretching"
    };

    /// <summary>Shade class for a 1-based group index; null for warm-up and stretching.</summary>
    public static string? GroupShade(int groupIndex)
        => groupIndex <= 0 ? null : $"grp-{((groupIndex - 1) % ShadeCount) + 1}";

    public static string PhaseIcon(ProgramPhase phase) => phase switch
    {
        ProgramPhase.WarmUp => "🔥",
        ProgramPhase.Main => "💪",
        _ => "🧘"
    };
}
