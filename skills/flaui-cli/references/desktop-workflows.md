# Desktop workflows

Use this reference when you need a repeatable desktop automation flow with `flaui-cli`.

## Launch a new application

`launch` starts the application, attaches the first visible top-level window, and writes a fresh snapshot automatically.

```powershell
flaui-cli launch notepad.exe
flaui-cli fill e3 "Hello from FlaUI CLI"
flaui-cli screenshot e1
flaui-cli close e1
```

Treat the first snapshot as your selector discovery step. Open the generated `.flaui-cli/snapshot-<timestamp>.yml` file and use the refs from that file for later actions.

## Attach to an existing application

`attach` performs a case-insensitive substring match on visible top-level desktop window titles.

```powershell
flaui-cli list
flaui-cli attach "Calculator"
flaui-cli snapshot
flaui-cli click e7
```

Use `list` first when the exact window title is uncertain.

## Read text and capture evidence

Use `get-text` when you need assertion-friendly output and `screenshot` when you need a visual artifact.

```powershell
flaui-cli get-text e3
flaui-cli screenshot e1
flaui-cli screenshot e12
```

Artifacts are written under `.flaui-cli/` in the current working directory.

## Focus or close the root window

The root window ref in a fresh snapshot is typically `e1`.

```powershell
flaui-cli focus e1
flaui-cli close
```

`close` defaults to the root window when no ref is provided.
