namespace Poe2ConfigDoctor.Logs;

/// <summary>Tallies of the known problem signatures found in a log (or a single session of one).</summary>
public sealed class IssueCounts
{
    /// <summary>STREAMLINE / DLSS out-of-VRAM warnings (<c>eWarnOutOfVRAM</c>).</summary>
    public int VramOom { get; set; }

    /// <summary>D3D12 "Device Removed" events — a GPU device loss (DXGI_ERROR_DEVICE_REMOVED).</summary>
    public int Dx12DeviceRemoved { get; set; }

    /// <summary>"Pipeline generation failed" — DX12 PSO creation failures.</summary>
    public int PipelineGenerationFailed { get; set; }

    /// <summary>"Shader uses incorrect vertex layout" render errors.</summary>
    public int ShaderVertexLayout { get; set; }

    /// <summary>"Abnormal disconnect" — often downstream of a device loss / freeze.</summary>
    public int AbnormalDisconnect { get; set; }

    public bool Any =>
        VramOom > 0 || Dx12DeviceRemoved > 0 || PipelineGenerationFailed > 0
        || ShaderVertexLayout > 0 || AbnormalDisconnect > 0;
}
