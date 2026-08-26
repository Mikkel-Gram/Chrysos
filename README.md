# Chrysos

A time based home training app: build your own workout programs, or let the app generate a
random session for you — always limited to exercises you can actually do with the equipment
you own. Blazor WebAssembly, installable as a PWA, works fully offline.

## Features

- **Random program generator** — pick session length, difficulty, focus muscle group(s), the
  warm up / strength / cardio / stretching mix and how many sets each work group runs for
  (1, 2, 3 or random). Difficulty scales every exercise's duration and filters out exercises above
  your intensity ceiling. Programs are always ordered warm-up first, stretching last, with strength
  and cardio interleaved through the middle.
- **Groups and sets** — the work block is split into circuits of 3-4 exercises. Every exercise in a
  group is performed once, then the group repeats for the chosen number of sets before moving on to
  the next group. There is a small chance the generator leaves the whole work block as one group.
  Warm-up and stretching are always performed once. The program overview and the session player show
  which phase, group and set you are in.
- **Equipment aware** — tick what you own in settings; nothing you can't do is ever offered.
- **Exercise library** — 74 standard exercises with category, intensity, high level muscle group,
  specific muscle, required equipment, alternating (left/right) flag and default duration.
  Add, edit and delete freely; restore the standard set at any time from settings.
- **Combos** — sequences performed for one side and then repeated on the other, kept on their own
  tab in the library. Combos can be picked by the generator too.
- **Program library** — build programs manually from library items (including grouping work
  exercises into circuits and choosing sets per group), or save a generated session after
  finishing it.
- **Session player** — a lead-in countdown before the first exercise, then work intervals separated
  by a single combined rest/countdown step that shows (and previews) the next exercise, marks
  "switch sides" transitions, beeps in the last three seconds, keeps the screen awake, and supports
  pause/skip/back.
- **History** — the last 30 sessions, with "do it again" and "save to library".

Everything is time based; no repetition counting anywhere.

## Videos

The player shows the exercise name today. Drop a file into `wwwroot/videos/` and set the
exercise's **Video URL** (for example `videos/push-up.mp4`) — the session player and the exercise
detail page then show the video instead, with the name as fallback.

## Data

All data lives in the browser's `localStorage` on the device, under these keys:

| Key | Contents |
| --- | --- |
| `chrysos.exercises` | exercise library |
| `chrysos.combos` | combo library |
| `chrysos.programs` | saved programs |
| `chrysos.history` | session history (capped at 30) |
| `chrysos.settings` | equipment, difficulty, rest/countdown, lead-in, sound |
| `chrysos.currentSession` | the running session, so a reload does not lose it |

Data saved by earlier versions under the `ge.` prefix is migrated automatically on first read.

There is no server and no account — nothing leaves the device.

## Run

```powershell
dotnet run
```

Then open the printed URL.

## Publish

```powershell
dotnet publish -c Release
```

The static site ends up in `bin/Release/net10.0/publish/wwwroot` and can be hosted on any static
web host (GitHub Pages, Azure Static Web Apps, nginx…).

### Deploying to GitHub Pages

`.github/workflows/deploy.yml` publishes the app to GitHub Pages. It runs when a **GitHub Release
is published**, and can also be triggered by hand from the Actions tab ("Run workflow").

One-time setup in the repository: **Settings → Pages → Build and deployment → Source: GitHub
Actions**.

The site is then served from `https://<user>.github.io/Chrysos/`. Three things make that sub path
work, all handled by the workflow:

| Concern | How it is handled |
| --- | --- |
| Asset base path | `<base href>` is rewritten to `/Chrysos/` **before** `dotnet publish` |
| Client-side routes | `index.html` is copied to `404.html`, so a hard refresh on e.g. `/Chrysos/library` still boots the app |
| `_framework` folder | An empty `.nojekyll` file stops Jekyll from stripping underscore-prefixed folders |

The base href must be set *before* publishing: the service worker asset manifest records a hash of
`index.html`, so editing it afterwards fails the integrity check and the service worker silently
refuses to install — which would quietly kill offline support.

If you move to a custom domain (or a `<user>.github.io` repo), set `BASE_PATH: /` at the top of the
workflow.

## Project layout

```
Models/     domain types (Exercise, Combo, WorkoutProgram, UserSettings, …)
Data/       SeedData.cs — the standard library, with stable ids
Services/   storage, library, program library, history, generator, session building
Pages/      Home, Generate, Session, Programs, Library, History, Settings + editors
Shared/     small reusable components (equipment picker, confirm dialog, media frame)
```
