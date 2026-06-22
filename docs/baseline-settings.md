# PoE2 Baseline Settings

A recommended, safe baseline for `poe2_production_Config.ini` — the common fixes that the
community and benchmarks consistently agree on, with rationale and trade-offs. Tuned toward
**stability and smooth frame-times on mid-range / 8 GB-VRAM GPUs at 1440p**, which is where most
PoE2 performance complaints come from.

All values are config-file edits (no anti-cheat risk). Driver/OS items are listed separately at the
end because they live *outside* the config and the tool can only advise on them.

> **Important:** the in-game graphics menu rewrites the whole config on Apply and silently resets
> some keys to a preset. After visiting it, re-check the config (this is exactly the drift the tool
> caught more than once). Edit the file with the game closed.

## The baseline (config keys)

Ordered by impact. "Why" is the consensus from GGG forums, benchmarks, and the research catalog
([`known-issues.md`](known-issues.md)).

| Key | Baseline value | Why | Trade-off |
|-----|---------------|-----|-----------|
| `global_illumination_detail` | `0` | **#1 FPS lever** — GI re-lights per spell cast; off = ~+40 FPS / ~32% (patch 0.5.0 benchmark) | Loses dynamic spell lighting (mostly cosmetic) |
| `renderer_type` | `Vulkan` *or* `DirectX12` — **rig-dependent, test both** | The only GGG-confirmed key. No universal winner; pick the one that doesn't crash on *your* hardware (F1 overlay) | DX11 not confirmed for PoE2 — don't guess it |
| `texture_quality` | `TextureQualityMedium` (→ `…Low` if VRAM saturates) | Largest **VRAM** consumer; over-budget = texture culling stutter | Blurrier textures |
| `upscale` / `upscale_resolution` | `DLSS` / `Quality` | ~20–30% "free" FPS + smooths frametime spikes; DLSS Quality > FSR (spatial) > NIS | Slight softening |
| `shadow_type` | `Medium` | Meaningful VRAM + fewer shader/PSO hitches | Coarser shadows |
| `hdr` | `false` | Larger framebuffers; off unless you have a genuinely good HDR display | No HDR |
| `use_dynamic_particle_culling2` | `true` | Culls distant/off-screen particles — helps the dense-content CPU load (note the `2`) | Subtle pop of far effects |
| `screenspace_effects` | low (`1`) | SSR/SSAO cost; also improves loot/enemy clarity | Flatter look |
| `bloom_strength` | low (`0.25`) | Cheap, and reduces visual noise | Less glow |
| `use_dynamic_resolution` | `false` when using DLSS | Don't stack upscaler + dynamic-res ("both = mud") | — |
| `vsync` | `Off` (with G-Sync/VRR) | Lowest latency inside the VRR window | Tearing if VRR off |
| `triple_buffering` | `false` | Unneeded with VRR + vsync off | — |
| `reflex_mode` | `LowLatency` (test `Off` if you see spikes) | Lower input latency; some report Reflex-related spikes | Rig-dependent |
| `framerate_limit` (+`framerate_limit_enabled=true`) | a stable cap **inside** your VRR range | Frametime *consistency* beats a high jittery average; align to refresh (e.g. half of 144 = 72) and stay above the VRR floor | Caps peak FPS |
| `engine_multithreading_mode` | `enabled` (test `disabled` only if you get load/portal freezes) | Default; helps the CPU-bound engine — but its MT path causes freezes for some | Disabling can cut FPS hard |

### What "baseline" means vs. rig-specific
- **Universally safe** (apply anywhere): GI off, particle culling on, HDR off, sane frame cap, no upscaler+dynamic-res stacking.
- **Rig-specific** (must be chosen from evidence, not defaulted): `renderer_type` (depends which renderer crashes on your hardware), and how far down `texture_quality` must go (depends on VRAM headroom). This is why the tool's renderer/VRAM fixes are **log-driven** rather than a fixed preset.

## Verified config-key reference

These keys were confirmed **verbatim** from real `poe2_production_Config.ini` files (a captured EA-launch
dump in dxvk issue [#4530](https://github.com/doitsujin/dxvk/issues/4530) and this machine's own file).
Graphics keys are in `[DISPLAY]`; multithreading in `[GENERAL]`. GGG publishes no schema, so the full
set of accepted *values* per key is partly inferred from naming — flagged below.

| Key | Section | Type / known values |
|-----|---------|---------------------|
| `renderer_type` | DISPLAY | enum: `Vulkan`, `DirectX12` (GGG-confirmed); `DirectX11` unconfirmed |
| `texture_quality` | DISPLAY | enum: `TextureQualityLow` / `…Medium` / `…High` (Med/High inferred) |
| `texture_filtering` | DISPLAY | int (anisotropic level, e.g. `8`, `16`) |
| `shadow_type` | DISPLAY | enum: `Off`/`Low`/`Medium`/`High` (lower tiers inferred) |
| `light_quality` | DISPLAY | int tier |
| `global_illumination_detail` | DISPLAY | int tier (`0` = off) |
| `water_detail` | DISPLAY | int tier |
| `screenspace_effects` | DISPLAY | int tier |
| `screenspace_effects_resolution` | DISPLAY | int tier |
| `bloom_strength` | DISPLAY | numeric |
| `upscale` | DISPLAY | enum: `DLSS`/`FSR`/`XeSS`/`NIS`/off |
| `upscale_resolution` | DISPLAY | enum preset: `…`/`Balanced`/`Quality`/`UltraQuality`/`DLAA`/`Native` |
| `upscale_quality` | DISPLAY | enum: `Performance`/`Balanced`/`Quality` |
| `upscale_quality_xess` | DISPLAY | enum |
| `upscale_sharpness` | DISPLAY | float `0.0–1.0` |
| `hdr` | DISPLAY | bool |
| `vsync` | DISPLAY | enum: `Off`/`On`/`Adaptive` (**not** a bool) |
| `triple_buffering` | DISPLAY | bool |
| `reflex_mode` | DISPLAY | enum: `Off`/`LowLatency`/`LowLatencyBoost` |
| `framerate_limit` | DISPLAY | int (gated by `framerate_limit_enabled`) |
| `framerate_limit_enabled` | DISPLAY | bool |
| `background_framerate_limit` | DISPLAY | int (e.g. `30`) |
| `use_dynamic_resolution` | DISPLAY | bool (+ `dynamic_resolution_fps`) |
| `use_dynamic_particle_culling2` | DISPLAY | bool (literal `2` suffix) |
| `resolution_width` / `resolution_height` | DISPLAY | int |
| `fullscreen` / `borderless_windowed_fullscreen` | DISPLAY | bool |
| `engine_multithreading_mode` | GENERAL | enum: `enabled`/`disabled` (string, not 0/1) |

See [`known-issues.md`](known-issues.md#fabricated-config-keys--do-not-use) for keys that look real
but **don't exist** (`shadow_quality`, `streaming_cache_pool_size`, `fps_limit`, …).

## Driver / OS baseline (outside the config — advise, don't auto-apply)

- **NVIDIA Control Panel** (PoE2 exe profile): Shader Cache Size → 100 GB/Unlimited; Power Management → Prefer Max Performance; Low Latency → Ultra/On; Resizable BAR on (+ BIOS "Above 4G Decoding").
- **HAGS:** test it **off** (Settings → Display → Graphics → default graphics settings) if you get freezes/Device-Removed.
- **Windows:** High Performance power plan; set the PoE2 exe to "High Performance" GPU; keep game on NVMe/SSD; disable overlays if chasing crashes.
- **Don't:** frame-gen DLL mods (OptiScaler) or process-manipulation tools (PoEUncrasher, Process Lasso) — ban risk / anti-cheat exposure.

## Toward a `--baseline` mode in the tool

A future enhancement: `poe2doctor --baseline` would apply the **universally safe** subset above
(GI off, particle culling on, HDR off, no upscaler+dynamic-res stacking, a sane VRR-aligned frame cap),
while **leaving rig-specific keys (`renderer_type`, exact `texture_quality`) to the log-driven rules**.
It would, like `--apply` today, back up the config first, refuse while the game is running, and print
each change with its reason. The split matters: a baseline preset should never blindly pick a renderer
or texture tier — those depend on what *your* hardware and log say.
