# Chrysos — notes for coding agents

Blazor WebAssembly PWA for time-based home workouts. No backend, no database: everything
runs in the browser and all state lives in `localStorage`. Live at
<https://mikkel-gram.github.io/Chrysos/>.

## Quick facts

| | |
| --- | --- |
| Project file | `Chrysos.csproj` (`net10.0`, SDK 10.0.301) |
| Dev server | `dotnet run` → <http://localhost:5027> |
| Repo | `Mikkel-Gram/Chrysos` (public), default branch `main` |
| Local folder | `C:\Git\GoldenExercise` — **the old name, deliberately**. Renaming it would detach the session; nothing in the code depends on it. |
| Deploy | `.github/workflows/deploy.yml`, on published release + manual dispatch |

## Domain model

The three concepts that everything else hangs off:

- **Exercise** — one movement. Time-based, never reps. Carries category, intensity, muscle
  groups, required equipment, default duration and `Alternating` (whether it must be done
  per side).
- **Combo** — an ordered list of exercises done all-left, then all-right. Lives in the
  library alongside exercises but under a separate tab.
- **WorkoutProgram** — a flat, ordered `List<ProgramItem>`. Items are **snapshots** of
  library entries, so a saved program keeps working after the underlying exercise is edited
  or deleted. Don't "fix" this by storing references.

### Program structure is derived, not stored

`Items` stays flat. Structure comes from two fields plus a derived view:

- `ProgramItem.Category` → `ProgramItem.Phase` (WarmUp / Main / Stretching). Phase order is
  always warm-up → work → stretching; cardio and strength are interleaved inside the work
  phase.
- `ProgramItem.GroupIndex` + `Rounds` express circuit groups within the work phase.
- `WorkoutProgram.Segments()` folds consecutive items sharing a phase + group into
  `ProgramSegment`s. Totals (`WorkSeconds`, `WorkStepCount`, `TotalSeconds`) all derive from
  segments.

This was a deliberate choice to avoid refactoring every consumer of `Items`. Keep it.

**`NormalizeGroups()` must be called after any add / remove / move.** It renumbers groups
1..n, zeroes them for warm-up and stretching, and forces one `Rounds` value per group. It
compares each item's *original* `GroupIndex` against the previous item's original value
(captured before overwriting), so runs are detected correctly — don't simplify that.

`ProgramItem.Category` is nullable so programs saved before phases existed still load.

## Where things live

```
Models/     domain types; Enums.cs has every enum with [Display] names
Data/       SeedData.cs — 74 exercises + 6 combos, ids from a deterministic StableId() hash
Services/   storage, library, generator, session building
Pages/      routable components (@page)
Shared/     reusable components
Layout/     MainLayout
```

All services are registered as **singletons** in `Program.cs` (single-user client app).

### The three files that carry the real logic

- **`Services/ProgramGenerator.cs`** — candidate filtering and weighting, per-category time
  budget, `Interleave`, `AssignGroups`, and `CreateReplacement` (single-item ↻ swap; must
  *not* regenerate the whole program).
- **`Services/SessionBuilder.cs`** — expands a program into timed `SessionStep`s:
  segments × rounds × items × sides. A single combined transition precedes every work
  interval: a "Get ready" lead-in before the very first one, otherwise a rest step carrying
  `NextTitle` / `NextVideoUrl` so the player can preview what's coming.
- **`Shared/ProgramOutline.razor`** — the single source of truth for the program overview
  (phase headers, group sub-headers, optional per-row action). Used by both
  `ProgramPreview.razor` and `ProgramDetail.razor`. Change it once, both views update.
  `Shared/ProgramEquipment.razor` does the same for the "equipment needed" card those two
  pages show above the outline; it reads the equipment snapshot on each `ProgramStep` and
  falls back to the library for programs saved before that snapshot existed.

### Generating is a two-page flow

`Generate.razor` is only the form: generating stores the result in `DraftProgramState` and
navigates to `ProgramPreview.razor` (`/generate/preview`), which shows equipment, outline,
per-item swap, start and save. Regenerating is simply going back. The draft is persisted
(`chrysos.draft`) so reloading the preview keeps it, and returning to the form restores the
options that produced it.

### Generator behaviour worth knowing before you "fix" it

- Rounds are rolled **once per session** and apply per group, circuit style (A, B, C, then
  repeat).
- The work budget is **divided by the round count**, so a 30-minute session stays ~30 minutes
  regardless of sets. Consequence: more sets ⇒ fewer distinct exercises (often only 4–6), so
  a single work group is legitimately common. There's a UI hint about this — it is not a bug.
- `AssignGroups` picks `groupCount = max(1, round(n / 3.5))` and splits evenly. An earlier
  "don't leave a lonely trailing exercise" heuristic collapsed everything into one group and
  was removed. There is also a deliberate 5% chance of no grouping at all.

## Storage

Keys are `chrysos.*`: `settings`, `exercises`, `combos`, `programs`, `history`,
`currentSession`, `draft`. `BrowserInterop.GetAsync` transparently migrates the legacy `ge.*` keys
(read old → write new → delete old); leave that in place.

JS interop lives in `wwwroot/js/app.js`: localStorage get/set/remove, `beep`, wake lock and
`downloadJson`. Wake lock is best-effort and is denied in headless browsers — a warning there
is expected.

`Exercise.IsBuiltIn` marks seeded entries; `LibraryService.ResetToStandardAsync` restores the
`SeedData` library. Seed ids come from `StableId()`, a deterministic hash, so built-in ids never
change between runs — never replace them with `Guid.NewGuid()`.

## Gotchas that have cost real time

**PWA base path.** The app is served from a sub-path (`/Chrysos/`). The workflow rewrites
`<base href>` **before** `dotnet publish`, and this ordering is load-bearing: the service
worker asset manifest stores a hash of `index.html`, so editing it after publish fails the
integrity check and the service worker *silently* refuses to install, killing offline support
with no visible error. `service-worker.published.js` derives its base from
`self.registration.scope`, so it works at root or sub-path without patching.

**Razor.** A page `X.razor` cannot declare a member named `X` (CS0542). String literals inside
Razor attribute lambdas break parsing. `@onclick` lambdas must not be void-returning ternaries.

**Navigation** uses relative paths (`Nav.NavigateTo($"library/exercise/{id}")`) so the
sub-path works — don't add leading slashes.

**Windows PowerShell 5.1 + UTF-8.** `Get-Content -Raw` + `Set-Content` round-trips through the
ANSI codepage. The console *displays* mojibake but the bytes survive (cp1252 decode→encode is
byte-preserving). Prefer `[IO.File]::ReadAllText` / `WriteAllText` with
`New-Object Text.UTF8Encoding $false` when rewriting source files. The codebase uses emoji in
UI strings, so this matters.

**GitHub Actions indexing.** A brand-new repo may report `total_count: 0` from the workflows
API for a long time after the first push, and `gh workflow run` 404s. Pushing a commit that
*touches the workflow file itself* forces indexing.

**Pages environment.** Releases run on a tag ref, but the `github-pages` environment defaults
to allowing only the default branch — the build passes and the deploy is rejected. A tag rule
(`*`) is configured; if deploys start failing at the deploy step, check that first.

## Testing

There is no test project. Verification is done by driving a real browser with Playwright,
which is installed globally in `%TEMP%` — **test scripts must live in `%TEMP%`** or the module
won't resolve. Avoid non-ASCII in selectors (a `Search…` placeholder selector failed;
`input[placeholder^=Search]` works).

`#blazor-error-ui` is always in the DOM but hidden. Its text shows up in `textContent` scrapes
and is **not** an error — check `isVisible()` before believing it.

Useful checks when changing anything structural: generate a program and confirm phase headers
and group/set labels; step through a session and confirm circuit ordering and side handling;
hard-refresh a deep URL; and load once online then go offline and confirm the app still boots
and generates.

## Conventions

- Comment only what needs clarification; the codebase is deliberately light on comments.
- Exercises are always time-limited. If a feature implies reps, it is wrong.
- `Shared/ExerciseMedia.razor` is the placeholder for future exercise videos — the model
  already carries `VideoUrl` and `wwwroot/videos/` exists. Show the name when there's no video.
