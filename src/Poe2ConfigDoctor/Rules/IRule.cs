using Poe2ConfigDoctor.Config;
using Poe2ConfigDoctor.Logs;
using Poe2ConfigDoctor.Models;

namespace Poe2ConfigDoctor.Rules;

/// <summary>
/// A diagnostic rule: inspects the log scan plus current config and, if its condition holds,
/// returns a <see cref="Finding"/> (with proposed config changes). Returns null when it doesn't fire.
/// </summary>
public interface IRule
{
    string Id { get; }
    Finding? Evaluate(LogScanResult log, IniConfig config);
}
