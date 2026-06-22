namespace Poe2ConfigDoctor.Models;

/// <summary>A single proposed (or applied) edit to a config key, with the reason behind it.</summary>
/// <param name="Key">The config key, e.g. <c>renderer_type</c>.</param>
/// <param name="OldValue">The value found in the config, or null if the key was absent.</param>
/// <param name="NewValue">The value the tool wants to set.</param>
/// <param name="Reason">Plain-language justification shown to the user.</param>
public sealed record ConfigChange(string Key, string? OldValue, string NewValue, string Reason);
