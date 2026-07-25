<div align="center">

<img src="assets/logo.svg" alt="Win11 Tweaker" width="120" height="120">

# Win11 Tweaker

**English** · [Русский](README.ru.md)

Tweak, clean and debloat Windows 10 and 11 by answering plain questions
instead of hunting through a hundred toggles.

[win11tweaker.com](https://win11tweaker.com) · [Releases](https://github.com/touchest/win11tweaker/releases) · [GPL-3.0](LICENSE)

</div>

Configure, clean up and maintain Windows 10 and 11 without digging through a hundred
toggles. You answer plain questions («Do you use Xbox?», «Do you have a printer?») and
the app does the rest: shuts off services you do not need, clears junk, strips the
built-in bloat.

Every change lands in a log next to its old value, so anything is one click away from
undo. The whole thing is open, so you can check for yourself that it does nothing behind
your back.

## What it does

- **Setup wizard.** One questionnaire covers services, tweaks and built-in apps. It
  remembers your answers and asks only about what is new. Service dependencies are worked
  out in advance, so nothing you rely on gets switched off by accident.
- **System tweaks.** Context menu, File Explorer, taskbar, privacy and telemetry, Windows
  services, Defender, UAC, deeper registry keys.
- **Disk cleanup.** Temp files, update cache, logs, dumps, the WinSxS component store, the
  leftover Windows.old folder. It shows you the size first, then clears.
- **Bloat removal.** Out-of-the-box apps, OneDrive, Edge WebView2 along with its update
  services, self-repair blocked so nothing crawls back after an update.
- **Background upkeep.** Auto cleanup, memory relief when RAM runs tight, GPU cooling by
  temperature over NVML, startup checks.
- **Undo.** A change journal that keeps old values, plus a Windows restore point before a
  batch of tweaks.

## Stack

.NET 10 with WPF. Two projects:

| Project | Role |
| --- | --- |
| `Win11Tweaker.Core` | Knows nothing about the UI. Holds the tweak model. |
| `Win11Tweaker.App` | The WPF shell. |

They are split so the apply step can later move into an elevated broker process while the
window itself keeps running without admin rights.

A tweak is data, not code. `TweakDefinition` says what to write, on which builds, what it
conflicts with and how risky it is. A single engine applies all of them. State is always
read from the machine rather than assumed, and the case that matters most is `Drifted`:
the key is there, but we are not the ones who set it. On a well-worn Windows install that
turns up all the time.

## Build

You need the .NET SDK 10.

```
dotnet build Win11Tweaker.slnx
dotnet run --project src/Win11Tweaker.App
```

A single self-contained exe that runs without .NET installed:

```
dotnet publish src/Win11Tweaker.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## Notes

- Some tweaks need administrator rights, and service changes only settle after a reboot.
- GPU cooling control needs an NVIDIA card that exposes NVML. On anything else that section
  simply stays off.
- Built and tested on Windows 10 and 11, 64-bit. Older builds are not a target.
- Removing OneDrive and Edge WebView2 is deliberate and awkward to undo from inside the app,
  so make sure you actually want them gone before you do it.

## License

[GPL-3.0](LICENSE). Fork it, change it, but keep derivatives open under the same license.
