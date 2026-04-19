# flaui-cli

A CLI for automating Windows applications built on [FlaUI](https://github.com/FlaUI/FlaUI).

`flaui-cli` gives scripts and coding agents a simple command-line interface for driving Windows applications through UI Automation. It keeps a background daemon alive between invocations, remembers the currently attached window, and refreshes element refs through YAML snapshots.

This project is inspired by [playwright-cli](https://github.com/microsoft/playwright-cli).

## Key Features

- CLI-first workflow for Windows applications automation
- Built on FlaUI UIA3
- Background daemon keeps the attached window alive across commands
- Snapshot-based element refs such as `e1`, `e2`, `e3`
- Built-in screenshot and text extraction commands

## Requirements

- Windows x64
- .NET 10 SDK or newer
- A Windows application that exposes useful UI Automation data

## Getting Started

```powershell
dotnet tool install --global flaui-cli # Install the CLI globally
gh skill install zzzkan/flaui-cli flaui-cli # Install agent skills

flaui-cli --help
```

## State and Snapshots

`flaui-cli` uses an internal background daemon to preserve state between separate CLI invocations.

- The first command starts the daemon automatically.
- The daemon exits after 5 minutes of inactivity.
- `launch` or `attach` selects the managed window.
- `snapshot` writes the current accessibility tree to `.flaui-cli/snapshot-<timestamp>.yml`.
- The latest snapshot defines the valid element refs for the current attached window.
- If the UI changes and a ref no longer resolves, run `snapshot` again.

Snapshot entries look like this:

```yaml
- button "OK" [ref=e12]
```

In a fresh snapshot, the attached window itself is written first, so the root window ref is `e1`.

Artifacts are written to `.flaui-cli/` in the current working directory:

- `snapshot-*.yml`
- `screenshot-*.png`

## Demo

### Launch an application

Use Notepad as a quick smoke test:

```powershell
flaui-cli launch notepad.exe
```

This launches Notepad, attaches its first visible top-level window, and writes a snapshot file under `.flaui-cli/`.

Open the generated snapshot and find the ref for the editor control. Then continue with commands like these:

```powershell
flaui-cli fill e3 "Hello from FlaUI CLI"
flaui-cli get-text e3
flaui-cli screenshot e1
flaui-cli close e1
```

In that flow:

- `e3` is an example editor ref from the latest snapshot.
- `e1` is the root Notepad window from the latest snapshot.

### Attach to a running application

Attach to an already running application by matching a window title substring.

```powershell
flaui-cli list
flaui-cli attach "Calculator"
flaui-cli snapshot
```

`attach` performs a case-insensitive substring match against visible top-level desktop windows.

## Commands

### Window Management

```powershell
flaui-cli launch <filename> [--args "..."]
flaui-cli attach <title>
flaui-cli list
flaui-cli focus <ref>
flaui-cli close <ref>
```

- `launch` launches an application and attaches its first visible top-level window
- `attach` attaches a visible top-level desktop window by title substring
- `list` lists visible top-level desktop windows
- `focus` focuses a window from the latest snapshot
- `close` closes a window from the latest snapshot

### Tree and Artifacts

```powershell
flaui-cli snapshot
flaui-cli screenshot <ref>
flaui-cli get-text <ref>
```

- `snapshot` writes the current accessibility tree to `.flaui-cli/snapshot-*.yml`
- `screenshot` captures an element or window into `.flaui-cli/screenshot-*.png`
- `get-text` reads text content from an element

### Element Actions

```powershell
flaui-cli click <ref> [--button left|right] [--double]
flaui-cli fill <ref> <value>
```

- `click` clicks or double-clicks an element from the latest snapshot
- `fill` replaces the value of a text field

## Acknowledgements

- [playwright-cli](https://github.com/microsoft/playwright-cli)
- [FlaUI](https://github.com/FlaUI/FlaUI)
