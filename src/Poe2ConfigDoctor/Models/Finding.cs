namespace Poe2ConfigDoctor.Models;

/// <summary>The result of one rule firing: what was detected and the config changes it proposes.</summary>
public sealed class Finding
{
    public required string RuleId { get; init; }
    public required string Title { get; init; }
    public required Severity Severity { get; init; }
    public required string Detail { get; init; }

    /// <summary>Changes this finding wants to make. May be empty (e.g. issue detected but nothing left to tune).</summary>
    public List<ConfigChange> Changes { get; } = new();
}
