using Poe2ConfigDoctor.Config;
using Poe2ConfigDoctor.Logs;
using Poe2ConfigDoctor.Models;

namespace Poe2ConfigDoctor.Rules;

/// <summary>
/// Detects DirectX 12 instability — "Device Removed" (GPU device loss) and/or pipeline-generation
/// failures — and, when the config is set to DX12, switches the renderer to Vulkan.
/// </summary>
public sealed class Dx12CrashRule : IRule
{
    public string Id => "DX12-CRASH";

    public Finding? Evaluate(LogScanResult log, IniConfig config)
    {
        var c = log.Scope;
        bool deviceRemoved = c.Dx12DeviceRemoved > 0;
        bool pipelineProblems = c.PipelineGenerationFailed > 0 || c.ShaderVertexLayout > 0;
        if (!deviceRemoved && !pipelineProblems)
            return null;

        var details = new List<string>();
        if (deviceRemoved)
        {
            var reason = log.LastDeviceRemovedReason is { } r ? $" (Reason: {r}, DXGI_ERROR_DEVICE_REMOVED)" : "";
            details.Add($"{c.Dx12DeviceRemoved}x D3D12 'Device Removed'{reason} — a GPU driver-level device loss, i.e. the hard freeze/crash.");
        }
        if (c.PipelineGenerationFailed > 0)
            details.Add($"{c.PipelineGenerationFailed}x 'Pipeline generation failed' — DX12 PSO creation bug in the DX12 render path.");
        if (c.ShaderVertexLayout > 0)
            details.Add($"{c.ShaderVertexLayout}x 'Shader uses incorrect vertex layout'.");
        if (c.AbnormalDisconnect > 0)
            details.Add($"{c.AbnormalDisconnect}x 'Abnormal disconnect' — the connection error you see is downstream of the device loss.");

        // Only act when the config will actually launch on DX12 next time.
        var renderer = config.Get(Poe2Config.DisplaySection, Poe2Config.RendererType) ?? log.CurrentRenderer;
        bool onDx12 = renderer is not null
            && (renderer.Contains("DirectX12", StringComparison.OrdinalIgnoreCase)
                || renderer.Contains("DX12", StringComparison.OrdinalIgnoreCase));

        if (!onDx12)
        {
            // Crashes happened in the window, but the renderer is already off DX12 — report, don't change.
            return new Finding
            {
                RuleId = Id,
                Title = "DirectX 12 instability seen earlier — already mitigated",
                Severity = Severity.Info,
                Detail = $"Within the {log.ScopeName}, DX12 problems were logged:\n    "
                    + string.Join("\n    ", details)
                    + $"\n    Your renderer is already set to '{renderer}', so this is resolved. Keep it off DirectX 12.",
            };
        }

        var finding = new Finding
        {
            RuleId = Id,
            Title = "DirectX 12 renderer is crashing the GPU device",
            Severity = Severity.Critical,
            Detail = $"In the {log.ScopeName}:\n    " + string.Join("\n    ", details),
        };

        var old = config.Get(Poe2Config.DisplaySection, Poe2Config.RendererType);
        finding.Changes.Add(new ConfigChange(
            Poe2Config.RendererType, old, "Vulkan",
            "DX12 caused a device-removed crash and/or pipeline failures; Vulkan is stable on this hardware and degrades gracefully instead of crashing."));

        return finding;
    }
}
