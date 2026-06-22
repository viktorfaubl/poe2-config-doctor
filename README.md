# PoE2 Config Doctor

A small command-line tool that reads **Path of Exile 2** client logs, detects known
performance failures, and applies safe, explained fixes to the game's config file
(`poe2_production_Config.ini`).

It only ever edits the local config file — it never touches the game process or memory,
so it carries no anti-cheat risk.

## What it detects

| Rule | Signature in the log | Fix applied |
|------|----------------------|-------------|
| **DX12-CRASH** | `[D3D12] Device Removed` (DXGI_ERROR_DEVICE_REMOVED), `Pipeline generation failed`, `Shader uses incorrect vertex layout`, `Abnormal disconnect` while on DirectX 12 | `renderer_type` → `Vulkan` |
| **VRAM-OOM** | `[STREAMLINE] Error eWarnOutOfVRAM` (VRAM budget exceeded) | Trims the biggest VRAM consumers: `texture_quality` → Medium, `upscale_resolution` → Quality, `shadow_type` → Medium, `hdr` → false (each only if currently higher) |

Detection is based on a **rolling time window — the last 3 days by default** — so a single
trivial session (e.g. loading a hideout and quitting) can't hide problems from earlier in the
week. Whole-log totals are always reported alongside the in-scope counts for context.

Scope can be changed:

- `--since <dur>` — use a different window, e.g. `--since 12h`, `--since 7d`
- `--all` — consider the entire log
- `--session` — consider only the latest game session

## Usage

```
poe2doctor [options]

  --log <path>      Path to Client.txt        (auto-detected if omitted)
  --config <path>   Path to poe2_production_Config.ini (auto-detected if omitted)
  --since <dur>     Consider issues within this window: 3d, 72h, 90m (default: 3d)
  --all             Consider the entire log, ignoring the time window
  --session         Consider only the latest game session
  --apply           Write the proposed changes (default: dry run, shows changes only)
  --tail <N>        Scan only the last N lines of the log (default: whole file)
  --no-backup       Do not create a .bak before writing
  --force           Apply even if the game appears to be running
  -h, --help        Show this help
```

By default it performs a **dry run** — it prints what it would change and writes nothing.
Re-run with `--apply` to make the changes. A `.bak` of the config is written first unless
`--no-backup` is given.

**Close the game before applying** — PoE2 overwrites the config on exit, and changing the
renderer in the in-game menu also rewrites the whole file (and silently resets some settings
to a preset).

### Examples

```sh
# Analyze only (auto-detect paths)
poe2doctor

# Analyze a specific log
poe2doctor --log "E:\Games\PoE2\logs\Client.txt"

# Apply the fixes
poe2doctor --apply
```

## Exit codes

| Code | Meaning |
|------|---------|
| `0`  | Ran cleanly — no issues found, or changes applied |
| `2`  | Issues found in a dry run (nothing written) |
| `1`  | Could not run (bad args, missing files, game running) |

## Build

Requires the .NET 8 SDK (or newer).

```sh
dotnet build -c Release
dotnet run -c Release --project src/Poe2ConfigDoctor -- --help
```

The `samples/` folder contains a fixture log + config that trigger both rules
(use `--all` so the fixture's dates are always in scope):

```sh
dotnet run -c Release --project src/Poe2ConfigDoctor -- \
  --log samples/sample_Client.txt --config samples/sample_Config.ini --all
```

## How it works

1. **`LogAnalyzer`** streams the log once, tallying the known failure signatures for the
   whole file, the latest session, and a time window, and pulls out the renderer in use and
   the DeviceLocal VRAM size.
2. **Rules** (`IRule`) each inspect the scan plus the current config and, if their condition
   holds, return a `Finding` with proposed `ConfigChange`s and a plain-language reason.
3. **`IniConfig`** applies changes line-by-line, preserving comments, ordering, and unrelated
   keys, scoped to the `[DISPLAY]` section.

Adding a new rule is just a new `IRule` implementation registered in `Program.cs`.

## Background

Born out of debugging stutter and hard freezes on an RTX 3060 Ti (8 GB) at 1440p: the game's
own log was reporting `eWarnOutOfVRAM` and, on DX12, `Device Removed` GPU crashes. The fixes
were always the same handful of config edits — so this automates the diagnosis and the edit.
