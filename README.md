# Abiotic Factor Dedicated Server

[![WindowsGSH](.github/assets/windowsgsh-badge.svg)](https://windowsgsh.com)
[![Status](https://img.shields.io/badge/status-needs_live_test-F59E0B)](#status)
[![Module version](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fraw.githubusercontent.com%2FWindowsGSH%2FWindowsGSH.AbioticFactor%2Fmain%2FAbioticFactor.mod%2Fmodule.json&query=%24.version&prefix=v&label=module&color=0F766E)](AbioticFactor.mod/module.json)
[![Requires WindowsGSH](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fraw.githubusercontent.com%2FWindowsGSH%2FWindowsGSH.AbioticFactor%2Fmain%2FAbioticFactor.mod%2Fmodule.json%3Fbadge%3Dminimum&query=%24.minimumWindowsGshVersion&prefix=v&label=requires%20WindowsGSH&color=2563EB)](AbioticFactor.mod/module.json)
[![Licence](https://img.shields.io/badge/licence-MIT-64748B)](LICENSE.md)

This module installs, launches, imports, monitors, and backs up Abiotic Factor dedicated servers.

## Status
**NEEDS LIVE TEST.** Current Steam identity, executable, launch parameters, paths, and ports pass static host validation. End-to-end behavior requires a current live server.

## Installation
WindowsGSH installs anonymous SteamCMD app `2857200` and launches `AbioticFactor/Binaries/Win64/AbioticFactorServer-Win64-Shipping.exe` directly.

## Configuration
WindowsGSH passes server name/password, world name, player limit, game port, and query port as individual process tokens. Module-owned switches cannot be overridden through Additional Arguments. Sandbox settings remain vendor-managed below `AbioticFactor/Saved`; unknown game settings are not overwritten.

## Networking
| Purpose | Default | Protocol | Exposure |
| --- | ---: | --- | --- |
| Game traffic | `7777` | UDP | Public; firewall/UPnP eligible |
| Steam query | `27015` | UDP | Public; firewall/UPnP eligible |

## Query, console, and administration
WindowsGSH currently reports process state. A2S, RCON, interactive console input, and player counts are not advertised until live-tested. Output is captured for diagnostics.

## Files and backups
- Executable: `AbioticFactor/Binaries/Win64/AbioticFactorServer-Win64-Shipping.exe`
- Configuration, worlds, administrator list, and logs: below `AbioticFactor/Saved`
- Backup target: complete `AbioticFactor/Saved`

## Known limitations
- No proven graceful headless shutdown channel is advertised.
- Imported launch settings cannot be recovered reliably from arbitrary historical batch files, so preview uses defaults.
- Player counts above the vendor-recommended six may display a warning and require live capacity testing.

## Beta verification checklist
- [ ] Fresh-install/update app `2857200` and verify its executable.
- [ ] Test names/passwords with spaces and confirm redacted diagnostics.
- [ ] Start, attach, restart WindowsGSH, stop, and confirm world integrity.
- [ ] Verify browser discovery, direct joining, UDP ports, and player count behavior.
- [ ] Test direct and WindowsGSM `serverfiles` imports using Copy and Adopt.
- [ ] Back up and restore a disposable world and sandbox configuration.

## Support
Report issues through the [issue tracker](https://github.com/WindowsGSH/WindowsGSH.AbioticFactor/issues) with sanitized version and log details. Never post passwords, Steam IDs, or private worlds.

## Support development
If you like the work I do and would like to support continued WindowsGSH and module development, you can contribute here:

- [Ko-fi](https://ko-fi.com/shenniko)
- [PayPal](https://paypal.me/shenniko)

## Trust and source
Modules execute with WindowsGSH's Windows permissions. Review [`AbioticFactorModule.cs`](AbioticFactor.mod/AbioticFactorModule.cs), [`module.json`](AbioticFactor.mod/module.json), and [SECURITY.md](SECURITY.md). Values were checked against the maintained [community dedicated-server reference](https://github.com/DFJacob/AbioticFactorDedicatedServer/wiki). Historical WindowsGSM repositories without a confirmed licence were not copied or adapted.

