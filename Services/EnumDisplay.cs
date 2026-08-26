using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Chrysos.Services;

public static class EnumDisplay
{
    private static readonly Dictionary<Enum, string> Cache = new();

    public static string Label(this Enum value)
    {
        if (Cache.TryGetValue(value, out var cached))
        {
            return cached;
        }

        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        var name = member?.GetCustomAttribute<DisplayAttribute>()?.Name ?? Humanize(value.ToString());
        Cache[value] = name;
        return name;
    }

    private static string Humanize(string value)
    {
        var chars = new List<char>();
        for (int i = 0; i < value.Length; i++)
        {
            if (i > 0 && char.IsUpper(value[i]) && !char.IsUpper(value[i - 1]))
            {
                chars.Add(' ');
            }

            chars.Add(value[i]);
        }

        return new string(chars.ToArray());
    }

    public static IEnumerable<T> Values<T>() where T : struct, Enum => Enum.GetValues<T>();

    public static string Duration(int totalSeconds)
    {
        var ts = TimeSpan.FromSeconds(totalSeconds);
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}h {ts.Minutes}m"
            : ts.Minutes > 0
                ? $"{ts.Minutes}m {ts.Seconds:00}s"
                : $"{ts.Seconds}s";
    }

    public static string Clock(int totalSeconds)
    {
        var ts = TimeSpan.FromSeconds(Math.Max(0, totalSeconds));
        return $"{(int)ts.TotalMinutes:00}:{ts.Seconds:00}";
    }
}
