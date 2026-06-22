using Poe2ConfigDoctor.Config;
using Poe2ConfigDoctor.Logs;
using Poe2ConfigDoctor.Models;

namespace Poe2ConfigDoctor.Rules;

/// <summary>
/// Flags long single sessions as a proxy for the documented post-0.3 system-RAM leak, which causes
/// progressive stutter/freezes over 1-2h (distinct from VRAM exhaustion). Behavioral/OS — advisory only.
/// </summary>
public sealed class LongSessionRule : IRule
{
    private static readonly TimeSpan Threshold = TimeSpan.FromHours(2);

    public string Id => "LONG-SESSION";

    public Finding? Evaluate(LogScanResult log, IniConfig config)
    {
        if (log.LongestSessionInScope is not { } longest || longest < Threshold)
            return null;

        return new Finding
        {
            RuleId = Id,
            Title = "Long play sessions — watch for the post-0.3 RAM leak",
            Severity = Severity.Info,
            Detail = $"Longest session in the {log.ScopeName} spanned {longest.TotalHours:0.0}h (wall-clock; may "
                + "include time the game was left idle/AFK). PoE2 has a documented post-0.3 system-RAM leak that "
                + "causes progressive stutter/freezes over 1-2h of play — distinct from VRAM exhaustion, and not config-fixable.\n    "
                + "If you get stutter late in a session, restart the game periodically and confirm the cause by "
                + "watching system RAM in Task Manager (if it climbs steadily, it's this leak, not VRAM).",
        };
    }
}
