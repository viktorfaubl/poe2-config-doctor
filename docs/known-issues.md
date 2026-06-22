# PoE2 Known Performance Issues — Catalog & Solutions

A catalog of documented Path of Exile 2 performance problems (Early Access, Dec 2024 → 2026),
compiled from GGG official forums, GGG's official posts, Steam discussions, vendor (NVIDIA/AMD/Intel)
forums, Microsoft docs, and cross-corroborated tech outlets. Each entry notes **whether this tool
can fix it**.

Every fix listed here is safe: config-file edits, driver settings, or OS settings only. Nothing
touches the game process or memory, so there is **no anti-cheat risk**. Tools that manipulate the
running process (CPU-affinity scripts, `PoEUncrasher`, Process Lasso/BES) are deliberately excluded,
as is OptiScaler-class frame-gen DLL injection (flagged as a ban risk in GGG forum reports).

## Fixability legend

| Tag | Meaning |
|-----|---------|
| 🟢 **AUTO** | This tool detects and fixes it via `poe2_production_Config.ini` today |
| 🟡 **CONFIG** | Fixable in the config / in-game graphics menu, not yet automated (candidate rule) |
| 🔵 **DRIVER/OS** | Fixable via GPU driver or Windows settings — outside the config file |
| ⚪ **GAME/SERVER** | Engine or server-side; only mitigable client-side (GGG must fix the root cause) |

## Summary

| # | Problem | Category | Fixability | Tool |
|---|---------|----------|-----------|------|
| 1 | First-encounter shader-compile hitches | Stutter | ⚪ / 🔵 | mitigate |
| 2 | Driver shader cache too small / evicted | Stutter | 🔵 DRIVER/OS | — |
| 3 | Stale shader cache after patch/driver update | Stutter | 🟡 CONFIG* | candidate |
| 4 | Renderer choice (DX12 vs Vulkan vs DX11) | Stutter/Crash | 🟢 AUTO | `DX12-CRASH` |
| 5 | VRAM exhaustion on 8 GB cards | VRAM | 🟢 AUTO | `VRAM-OOM` |
| 6 | Post-0.3 system-RAM leak (long sessions) | Memory | ⚪ GAME | advisory |
| 7 | D3D12 "Device Removed" (0x887A0005) | Crash | 🟢 AUTO | `DX12-CRASH` |
| 8 | Full client freezes / "Deadlock Detected" | Crash | 🟢/🔵 mix | partial |
| 9 | HAGS conflicts | Crash/Stutter | 🔵 DRIVER/OS | advisory |
| 10 | TDR device-reset timeout | Crash | 🔵 DRIVER/OS | — |
| 11 | Driver-version-specific crashes | Crash | 🔵 DRIVER/OS | — |
| 12 | "Unexpected disconnection" | Disconnect | ⚪ / 🟢 | diagnostic |
| 13 | CPU bottleneck in dense content | FPS | ⚪ / 🟡 | — |
| 14 | Global Illumination cost (#1 FPS lever) | FPS | 🟡 CONFIG | candidate |
| 15 | Particle/effect density (Breach/Ritual/bosses) | FPS | 🟡 CONFIG | candidate |
| 16 | Windows 11 24H2 loading-screen freeze | OS | 🔵 DRIVER/OS | — |

\* File maintenance (deleting cache folders), not a config key — but a safe action the tool could automate.

---

## Stutter & shader compilation

### 1. First-encounter shader-compilation hitches — ⚪/🔵
- **Symptom:** A brief freeze the first time a new skill effect, monster, or area appears. Smooth on the second pass. The F1 overlay's "Shader" line spikes during the hitch.
- **Cause:** PoE2 compiles shader pipeline objects on-the-fly, on the main thread, the first time an effect is rendered (community consensus; not an explicit GGG technical statement).
- **Fix:** Can't eliminate the *first* compile. Stop it recurring by keeping the shader cache healthy (#2, #3) and pre-warming (play through content; the cache fills and the second pass is smooth). Install on NVMe so asset-streaming hitches don't compound it.
- **Tool:** Not directly fixable, but renderer choice (#4) changes severity.
- **Sources:** GGG forum [3800105](https://www.pathofexile.com/forum/view-thread/3800105), [3592350](https://www.pathofexile.com/forum/view-thread/3592350), [3935550](https://www.pathofexile.com/forum/view-thread/3935550) (all community).

### 2. NVIDIA driver shader cache too small / evicted — 🔵 DRIVER/OS
- **Symptom:** Shader hitches that *recur* rather than tapering off, as if already-smooth content starts hitching again.
- **Cause:** The NVIDIA driver's own compiled-shader cache (`%LOCALAPPDATA%\NVIDIA\DXCache\`) fills and evicts entries, forcing recompiles. Historical default cap ~10 GB.
- **Fix:** NVIDIA Control Panel → Manage 3D Settings → **Shader Cache Size → 100 GB / Unlimited**. (Recent drivers may auto-manage this and hide the option.)
- **Sources:** GGG forum [3800105](https://www.pathofexile.com/forum/view-thread/3800105); Steam guide [3403877799](https://steamcommunity.com/sharedfiles/filedetails/?id=3403877799).

### 3. Stale shader cache after a patch / driver update — 🟡 CONFIG* (file maintenance)
- **Symptom:** New or worse stutter, or mass shader reloads, specifically *after* a game patch or GPU driver update.
- **Cause:** A mismatched/stale on-disk cache; entries are invalid and get rejected, forcing recompiles. A 0.5-era bug had shaders re-caching on every map/town entry.
- **Fix:** Close the game and delete the cache folders, then let them rebuild (expect ~30 min of worse stutter):
  - `%APPDATA%\Path of Exile 2\ShaderCacheVulkan`
  - `%APPDATA%\Path of Exile 2\ShaderCacheD3D12`
  - optionally `%LOCALAPPDATA%\NVIDIA\DXCache\`
- **Caveat:** Not guaranteed — several users report it didn't help. The often-cited `%LOCALAPPDATA%\Path of Exile 2\ShaderCache` path is **wrong** (doesn't exist; cache lives under Roaming).
- **Tool:** Candidate feature — a safe `--clear-shader-cache` action.
- **Sources:** GGG forum [3800105](https://www.pathofexile.com/forum/view-thread/3800105), [3935550](https://www.pathofexile.com/forum/view-thread/3935550), [3838854](https://www.pathofexile.com/forum/view-thread/3838854).

### 4. Renderer choice: DirectX 12 vs Vulkan vs DirectX 11 — 🟢 AUTO (`DX12-CRASH`)
- **Symptom:** Different stutter, and sometimes launch crashes or memory stalls, depending on the active renderer.
- **Cause / what's established:** The renderer is switchable via **`renderer_type`** — the one config key **officially confirmed by GGG** (values `Vulkan`, `DirectX12`). **There is no universally "best" renderer; they fail on different hardware:**
  - GGG officially offers `DirectX12` as the fix when **Vulkan crashes on launch**.
  - Vulkan is associated with **memory-pressure stalls**, notably on AMD RX 7000.
  - DX12 is associated with the **Device Removed crash family** (#7) on other rigs.
  - `DirectX11` is **not confirmed** as a valid config value for PoE2 — treat PoE2 as **DX12 + Vulkan only**; if testing DX11, select it via the in-game menu rather than guessing the string.
- **Fix:** Test both and keep the more stable frame-time (F1 overlay). **Don't toggle frequently** — each renderer rebuilds its own shader cache.
- **Tool:** The `DX12-CRASH` rule reacts to *your* log — if it sees Device Removed / pipeline failures while on DX12, it sets `renderer_type=Vulkan`. This is the right design precisely *because* the best renderer is rig-dependent: the tool follows evidence rather than dogma. **On this machine, DX12 crashed and Vulkan is stable — the opposite of the generic web advice.**
- **Sources:** Official [@PathOfExile](https://x.com/pathofexile/status/1908227562733797720) (~Apr 2025); GGG forum [3898265](https://www.pathofexile.com/forum/view-thread/3898265); dxvk issue [#4530](https://github.com/doitsujin/dxvk/issues/4530).

---

## VRAM & memory

### 5. VRAM exhaustion on 8 GB cards — 🟢 AUTO (`VRAM-OOM`)
- **Symptom:** Micro-stutter entering zones or on large spawns; textures go blurry mid-fight; pop-in; on 8 GB cards at 1440p/4K. Log shows `[STREAMLINE] Error eWarnOutOfVRAM`.
- **Cause:** Near ~95% VRAM the engine culls high-res textures (and spills to system RAM over PCIe) to avoid a crash; the swapping is the hitch. GGG has called the VRAM cap "intended behaviour" (shared engine, PoE1 context).
- **Fix (most → least VRAM saved):** lower **`texture_quality`** (dominant lever) → lower **`upscale_resolution`** (Balanced/Performance) → **`global_illumination_detail=0`** → lower **`shadow_type`** → **`hdr=false`** → lower output resolution. Target ≤ 80% VRAM. DLSS also saves ~200 MB/tier (minor vs textures).
- **Tool:** The `VRAM-OOM` rule applies exactly these (texture → Medium, upscale → Quality, shadow → Medium, hdr → false), each only if currently higher.
- **Note:** `eWarnOutOfVRAM` is not corroborated in public sources — it's a real token in *this* machine's log but treat it as a local signal. The VRAM-culling mechanism it reflects is well-documented.
- **Sources:** GGG dev tracker [3406695](https://www.pathofexile.com/forum/view-thread/3406695); GGG perf guide [3852015](https://www.pathofexile.com/forum/view-thread/3852015).

### 6. Post-0.3 system-RAM memory leak — ⚪ GAME (advisory)
- **Symptom:** Smooth for 10–30 min, then progressive stutter / multi-second lag spikes / freeze; system RAM climbs to 12–20 GB+ over ~2h, even AFK in hideout. Affects high-end rigs with 32–64 GB too.
- **Cause:** Community-attributed regression "introduced with patch 0.3"; no GGG root-cause. **Distinct from VRAM exhaustion** but presents identically.
- **Fix:** Restart the game periodically (only known mitigation). Not config-fixable.
- **How to tell apart from VRAM (#5):** watch **Task Manager**. If **system RAM** climbs steadily → this leak. If **VRAM** saturates → #5.
- **Tool:** Could add an advisory if the log shows a very long single session (no rule can see RAM, but session duration is a proxy).
- **Sources:** GGG forum [3836381](https://www.pathofexile.com/forum/view-thread/3836381).

---

## GPU crashes, freezes & disconnects

### 7. D3D12 "Device Removed" / DXGI_ERROR_DEVICE_REMOVED (0x887A0005) — 🟢 AUTO (`DX12-CRASH`)
- **Symptom:** Crash to desktop or black screen; log shows `[D3D12] Device Removed. Reason: 0x887a0005`, often after `Failed to create resource for texture` or `Pipeline creation failed`. (`0x887A0006` = DEVICE_HUNG.)
- **Cause:** Windows forcibly disconnected the GPU from D3D12 — a driver fault or GPU timeout (TDR, #10). Leading trigger is **VRAM exhaustion / memory pressure** during shader/texture work; also overlays hooking the DX12 present path.
- **Fix:** Lower `texture_quality`; **switch renderer DX12 → Vulkan**; update/roll back driver; clear shader cache; disable overlays; A/B test Reflex.
- **Tool:** The `DX12-CRASH` rule sets `renderer_type=Vulkan` when it sees this on DX12. This is the exact crash that hit this machine (`0x887a0005` in a level-79 map).
- **Sources:** GGG forum [3601905](https://www.pathofexile.com/forum/view-thread/3601905), [3795554](https://www.pathofexile.com/forum/view-thread/3795554); [KeenGamer D3D12 guide](https://www.keengamer.com/articles/features/troubleshooting/6-ways-to-fix-the-path-of-exile-2-poe-2-d3d12-error-on-windows-pcs/).

### 8. Full client freezes / "Deadlock Detected" — 🟢/🔵 mix
- **Symptom:** 10–30 s freeze + black screen with spinning gears on area load/zone transition/heavy combat, then recovery — or a hard hang. "Deadlock Detected" surged with Patch 0.5.
- **Cause:** Same TDR family as #7; engine multithreading race conditions under load; VRAM overflow without graceful throttling; HAGS conflicts; corrupt shader cache. One post-0.5 "Deadlock Detected" was tied to overly long character names (engine bug — use a short name).
- **Fix:** Switch to Vulkan; disable Reflex; lower textures; clear shader cache + reboot; disable HAGS (#9); update/roll back driver. **Engine multithreading:** the real key is **`engine_multithreading_mode`** (`enabled`/`disabled`) — disabling can stop some load/portal freezes but may cut FPS sharply; test both.
- **Tool:** Renderer switch covered by `DX12-CRASH`; multithreading toggle is a candidate rule (advisory, since it's a trade-off).
- **Sources:** GGG forum [3726278](https://www.pathofexile.com/forum/view-thread/3726278), [3595525](https://www.pathofexile.com/forum/view-thread/3595525).

### 9. HAGS (Hardware-Accelerated GPU Scheduling) conflicts — 🔵 DRIVER/OS (advisory)
- **Symptom:** Stutter, CPU spikes, freezes, contributes to "Deadlock Detected." HAGS is on by default on Win11.
- **Cause:** HAGS moves GPU work-scheduling to the GPU's hardware scheduler; on some Win11 systems it conflicts with PoE2's memory management.
- **Fix:** Settings → System → Display → Graphics → "Change default graphics settings" → toggle **Hardware-accelerated GPU scheduling** off → restart. Not universal — A/B test.
- **Tool:** Not a config key, but the tool could *advise* disabling HAGS when it detects the crash/freeze family (the log shows HAGS state at startup).
- **Sources:** GGG forum [3726278](https://www.pathofexile.com/forum/view-thread/3726278); [Microsoft TDR docs](https://learn.microsoft.com/en-us/answers/questions/4141570/).

### 10. TDR device-reset timeout — 🔵 DRIVER/OS (advanced, last resort)
- **Symptom:** The `0x887a0005` device-removed event itself; black-screen-gears recovery.
- **Cause:** Windows' watchdog resets the driver if the GPU doesn't finish work within `TdrDelay` (default 2 s).
- **Fix (advanced):** registry `HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers` → DWORD `TdrDelay = 10` → restart. **This masks a hanging GPU rather than fixing it** — try all other fixes first; back up the registry.
- **Sources:** [Microsoft Learn — TDR keys](https://learn.microsoft.com/en-us/answers/questions/4141570/).

### 11. Driver-version-specific crashes — 🔵 DRIVER/OS
- **Symptom:** Crashes that start or stop with a specific GPU driver release.
- **Findings (point-in-time; verify against current releases):** NVIDIA + Win11 24H2 — driver **572.16** cited as a fix (mixed reports). AMD — newer Adrenalin reportedly crashes; **25.8.1** cited as stable; one report blames a PoE2 Vulkan bug from 0.4 (switch API instead of downgrading). **Rule of thumb:** if crashes began right after a driver update, roll back.
- **Sources:** GGG forum [3714578](https://www.pathofexile.com/forum/view-thread/3714578), [3885290](https://www.pathofexile.com/forum/view-thread/3885290/page/9).

### 12. "Unexpected disconnection occurred" — ⚪/🟢 (diagnostic)
- **Symptom:** "An unexpected disconnection occurred"; mid-map it consumes the waystone, closes portals, fails the map.
- **Cause — important nuance:** **two distinct phenomena**, often conflated:
  1. **GPU-crash cascade** — a client freeze/TDR misses server keep-alives and the server drops it. Plausible but not GGG-confirmed.
  2. **Network/server-side** — entering portals during loading screens, gateway issues; **no GPU crash involved.**
- **Tell them apart:** check the log for `[D3D12] Device Removed` near the disconnect timestamp.
- **Fix:** If GPU-linked → apply #7/#8/#9. If network → switch login Gateway, set DNS 8.8.8.8, wired connection.
- **Tool:** The analyzer already counts `Abnormal disconnect` and proximity to Device Removed, so it can *attribute* the disconnect rather than fix it.
- **Sources:** GGG forum [3651812](https://www.pathofexile.com/forum/view-thread/3651812), [3833106](https://www.pathofexile.com/forum/view-thread/3833106).

---

## CPU-bound & FPS

### 13. CPU bottleneck in dense content — ⚪/🟡
- **Symptom:** High-end GPU underutilized; FPS tanks in juiced maps, dense packs, Breach/Ritual/Expedition, boss particle storms — regardless of GPU.
- **Cause:** PoE2 is **CPU-bound** — confirmed by GGG's own 0.4.0 patch notes (Dec 2025): multi-core optimizations for "at least a 25% boost… less frame spikes when CPU bound." The simulation/render submission doesn't fully parallelize.
- **Fix:** Mostly GGG's to fix. Client-side: reduce CPU-side work indirectly (particle/effect density, #15), cap framerate for frametime stability (#9 below), enable dynamic resolution for GPU-bound moments (won't help when CPU-bound). Keep `engine_multithreading_mode=enabled` unless it causes freezes.
- **Sources:** GGG 0.4.0 patch notes (Dec 12 2025); GGG perf guide [3852015](https://www.pathofexile.com/forum/view-thread/3852015).

### 14. Global Illumination cost — 🟡 CONFIG (the #1 FPS lever)
- **Symptom:** Large FPS drop with dynamic lighting on, especially during spell-heavy combat.
- **Cause:** GI re-lights surfaces per spell cast — expensive in PoE2's particle-dense fights.
- **Fix:** Set Lighting to "Shadows only" → **`global_illumination_detail=0`**. Benchmarked at ~+40 FPS / ~32% (patch 0.5.0). Single biggest FPS recovery.
- **Tool:** Strong candidate for a `--baseline` / FPS rule. (This machine already has it at 0.)
- **Sources:** [EZG patch 0.5.0 (+40 FPS)](https://www.ezg.com/blog/poe-2-patch-0-5-0-return-of-the-ancient-players-gaining-40-fps-one-simple-setting); [PCGamesN best settings](https://www.pcgamesn.com/path-of-exile-2/best-settings-pc).

### 15. Particle / effect density — 🟡 CONFIG
- **Symptom:** Worst FPS in screen-clearing endgame: Breach, Ritual, Expedition detonations, multi-particle bosses.
- **Cause:** Effect/particle count overwhelms CPU-side submission.
- **Fix:** Lower particle/effects quality; ensure **`use_dynamic_particle_culling2=true`** (note the literal `2`); lower `screenspace_effects`, `bloom_strength`.
- **Tool:** Candidate for the baseline preset.
- **Sources:** GGG perf guide [3852015](https://www.pathofexile.com/forum/view-thread/3852015).

### 16. Windows 11 24H2 loading-screen freeze — 🔵 DRIVER/OS
- **Symptom:** During zone transitions, CPU pins 100%, full lock-up needing hard reboot. Not RAM exhaustion (users have 32–64 GB free). Hits AMD X3D notably.
- **Cause:** Windows 11 **24H2** CPU-scheduling regression; absent on 23H2.
- **Fix:** Revert to 23H2; or restrict PoE2 CPU affinity off CPU0/1 (OS-level, not process injection); disable Engine Multithreading; Windowed Fullscreen; Defender exception.
- **Sources:** GGG forum [3603803](https://www.pathofexile.com/forum/view-thread/3603803).

---

## Fabricated config keys — DO NOT use

Every research agent independently confirmed these circulate on AI-generated SEO sites
(poe2settings.com and similar) but are **absent from every real `poe2_production_Config.ini`**,
including this machine's. They do nothing — don't add them:

`streaming_cache_pool_size` / `StreamingCachePoolSize` · `cache_directory` · `use_d3d12` ·
`directx_version` · `ground_decals` · `max_concurrent_voices` · `max_packet_size` ·
`fps_limit` / `target_fps` (real: `framerate_limit` + `framerate_limit_enabled`) ·
`global_illumination` (real: `global_illumination_detail`) · `shadow_quality` (real: `shadow_type`) ·
`lighting_quality` (real: `light_quality`) · `render_scaling` · `antialiasing` · `reflections` ·
`post_processing` · `screen_shake` (real: `screen_shake_v2`) · `engine_multithreading` (real:
`engine_multithreading_mode`) · `use_dynamic_particle_culling` (real: has a `2` suffix) ·
`networking_type=lockstep`.

There is **no config key** that controls shader-cache size or shader precompilation.

---

## What this tool covers vs. doesn't

- **Config fixes, applied (🟢):** renderer crashes on DX12 (`DX12-CRASH`), VRAM exhaustion (`VRAM-OOM`),
  FPS levers (`FPS-LEVERS`: GI off + particle culling), a safe **baseline preset** applied by default on
  `--apply`, and **shader-cache clearing** as a default `--apply` action (covers #3). GPU vendor is
  detected and used for a vendor-correct upscaler suggestion.
- **Advisories, detected & explained (🟡 → done):** the tool now finds the clue in the log and prints
  how to fix it, even when the fix is a driver/OS action it can't apply:
  - `DISCONNECT` (#12) — attributes each disconnect as GPU-crash cascade (near a Device-Removed) vs. network/server-side.
  - `ENGINE-MT` (#8) — suggests A/B testing `engine_multithreading_mode=disabled` when freezes appear and it's enabled.
  - `HAGS` (#9) — advises disabling Hardware-accelerated GPU scheduling when crashes appear and the log shows it on.
  - `LONG-SESSION` (#6) — flags long sessions as a proxy for the post-0.3 RAM leak; advises restart + Task Manager check.
- **Out of scope (🔵/⚪):** the *application* of driver settings (shader cache size, HAGS, TDR, driver version)
  and OS settings (24H2, power plan) — the tool advises but can't apply them. Server-side disconnects and the
  post-0.3 RAM leak are GGG's to fix.

See [`baseline-settings.md`](baseline-settings.md) for the recommended baseline config and the
verified key reference.
