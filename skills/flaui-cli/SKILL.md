---
name: flaui-cli
description: "Automates Windows desktop applications with flaui-cli for launching apps, attaching to windows, taking snapshots, clicking controls, filling fields, capturing screenshots, and extracting text. Use when the user needs to drive a Windows GUI, inspect the UI Automation tree, recover stale element refs, or automate a desktop workflow from the command line."
license: MIT
---

# Windows Desktop Automation with flaui-cli

## Quick start

```powershell
# list visible top-level windows
flaui-cli list
# launch an application and attach its first visible window
flaui-cli launch notepad.exe
# inspect the current UI tree and refs
flaui-cli snapshot
# interact with controls using refs from the latest snapshot
flaui-cli fill e3 "Hello from FlaUI CLI"
flaui-cli click e5
# capture the current window
flaui-cli screenshot e1
# close the attached window
flaui-cli close e1
```

## Commands

### Window management

```powershell
flaui-cli launch <filename> [--args "..."]
flaui-cli attach <title>
flaui-cli list
flaui-cli focus <ref>
flaui-cli close
flaui-cli close <ref>
```

### Tree and artifacts

```powershell
flaui-cli snapshot
flaui-cli screenshot <ref>
flaui-cli get-text <ref>
```

### Element actions

```powershell
flaui-cli click <ref> [--button left|right] [--double]
flaui-cli fill <ref> <value>
```

## State and snapshots

The first stateful command starts the background daemon automatically and creates a `.flaui-cli/` directory in the current working directory.

- The daemon exits after 5 minutes of inactivity.
- `launch` and `attach` select the managed window and immediately write a fresh snapshot.
- `click`, `fill`, and `focus` also refresh the snapshot after acting.
- `close` tries to refresh the snapshot after closing the target window, but that refresh can fail if the window disappeared successfully.
- The latest snapshot is the source of truth for valid refs such as `e1`, `e2`, and `e3`.

In a fresh snapshot, the attached window itself is written first, so the root window ref is typically `e1`.

When the UI changes and a ref no longer resolves, run `flaui-cli snapshot` again before retrying the next action.

## Local installation

If `flaui-cli` is not available globally, use a local tool manifest:

```powershell
dotnet new tool-manifest
dotnet tool install flaui-cli
dotnet tool run flaui-cli --help
```

## Example: Launch and fill Notepad

```powershell
flaui-cli launch notepad.exe
flaui-cli fill e3 "Hello from FlaUI CLI"
flaui-cli get-text e3
flaui-cli screenshot e1
```

## Example: Attach to a running application

```powershell
flaui-cli list
flaui-cli attach "Calculator"
flaui-cli snapshot
flaui-cli click e7
```

## Example: Recover from stale refs

```powershell
flaui-cli snapshot
flaui-cli click e12
# if the UI changed and e12 is no longer valid:
flaui-cli snapshot
flaui-cli click e18
```

## Specific tasks

- [Desktop workflows](./references/desktop-workflows.md)
- [Snapshot recovery](./references/snapshot-recovery.md)
- [Artifacts and installation](./references/artifacts-and-installation.md)
