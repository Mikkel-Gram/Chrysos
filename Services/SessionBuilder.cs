using Chrysos.Models;

namespace Chrysos.Services;

/// <summary>Expands a program into the flat list of timed steps used by the session player.</summary>
public static class SessionBuilder
{
    public static List<SessionStep> Build(WorkoutProgram program, UserSettings settings)
    {
        var steps = new List<SessionStep>();
        var leadIn = Math.Max(0, settings.CountdownSeconds);
        var rest = Math.Max(0, program.RestSeconds);
        var groupCount = program.GroupCount;
        var firstWork = true;

        foreach (var segment in program.Segments())
        {
            for (int round = 1; round <= segment.Rounds; round++)
            {
                for (int offset = 0; offset < segment.Items.Count; offset++)
                {
                    var item = segment.Items[offset];
                    if (item.Steps.Count == 0)
                    {
                        continue;
                    }

                    var itemIndex = segment.StartIndex + offset;
                    var sides = item.Alternating ? new[] { Side.Left, Side.Right } : new[] { Side.None };

                    foreach (var side in sides)
                    {
                        for (int stepIndex = 0; stepIndex < item.Steps.Count; stepIndex++)
                        {
                            var step = item.Steps[stepIndex];
                            var nextTitle = StepTitle(step.Name, side);

                            SessionStep Transition(SessionStepKind kind, string title, int seconds) => new()
                            {
                                Kind = kind,
                                Title = title,
                                DurationSeconds = seconds,
                                ItemIndex = itemIndex,
                                ItemName = item.Name,
                                Side = side,
                                Phase = segment.Phase,
                                GroupIndex = segment.GroupIndex,
                                GroupCount = groupCount,
                                Round = round,
                                TotalRounds = segment.Rounds,
                                NextTitle = nextTitle,
                                NextVideoUrl = step.VideoUrl
                            };

                            // One combined transition before every work interval: it is the rest period
                            // and the countdown to the next exercise at the same time.
                            if (firstWork)
                            {
                                if (leadIn > 0)
                                {
                                    steps.Add(Transition(SessionStepKind.GetReady, "Get ready", leadIn));
                                }

                                firstWork = false;
                            }
                            else if (rest > 0)
                            {
                                var startsRound = round > 1 && offset == 0 && stepIndex == 0 && side == sides[0];
                                var title = startsRound
                                    ? $"Rest — round {round} of {segment.Rounds}"
                                    : side == Side.Right && stepIndex == 0
                                        ? "Rest — switch sides"
                                        : "Rest";

                                steps.Add(Transition(SessionStepKind.Rest, title, rest));
                            }

                            steps.Add(new SessionStep
                            {
                                Kind = SessionStepKind.Work,
                                Title = step.Name,
                                Description = step.Description,
                                VideoUrl = step.VideoUrl,
                                Side = side,
                                DurationSeconds = step.DurationSeconds,
                                ItemIndex = itemIndex,
                                ItemName = item.Name,
                                Phase = segment.Phase,
                                GroupIndex = segment.GroupIndex,
                                GroupCount = groupCount,
                                Round = round,
                                TotalRounds = segment.Rounds
                            });
                        }
                    }
                }
            }
        }

        // Fill in "next up" labels for work steps.
        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i].Kind != SessionStepKind.Work)
            {
                continue;
            }

            var next = steps.Skip(i + 1).FirstOrDefault(s => s.Kind == SessionStepKind.Work);
            steps[i].NextTitle = next is null ? null : StepTitle(next.Title, next.Side);
        }

        return steps;
    }

    public static string StepTitle(string name, Side side)
        => side == Side.None ? name : $"{name} ({side})";
}
