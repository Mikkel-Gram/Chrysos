namespace Chrysos.Models;

/// <summary>
/// Bookkeeping that lets a newer app version merge its standard library into the copy already
/// stored in the browser without undoing the user's own decisions.
/// </summary>
public class LibraryState
{
    /// <summary>Built-in exercises the user deleted. They are not re-added by a merge.</summary>
    public List<Guid> RemovedBuiltInExerciseIds { get; set; } = new();

    /// <summary>Built-in combos the user deleted. They are not re-added by a merge.</summary>
    public List<Guid> RemovedBuiltInComboIds { get; set; } = new();

    /// <summary>
    /// Set once the stored library has been compared against the seed to detect edits made before
    /// <c>IsCustomized</c> existed. Without this the first merge would silently discard those edits.
    /// </summary>
    public bool CustomizationBackfillDone { get; set; }
}
