# Snapshot recovery

Use this reference when refs become stale or the managed window changed after a previous command.

## How refs stay valid

The latest snapshot file is the source of truth for refs such as `e1`, `e2`, and `e3`.

- `launch` and `attach` write a fresh snapshot automatically.
- `click`, `fill`, and `focus` also refresh the snapshot after they act.
- `snapshot` forces a manual refresh.
- `close` attempts a refresh after closing the window, but that refresh may fail because the target window is already gone.

## When to refresh manually

Run `flaui-cli snapshot` again when:

- the UI changed after navigation or modal dialogs
- a previously valid ref no longer resolves
- you reopened or reattached a different window

```powershell
flaui-cli snapshot
flaui-cli click e12
```

If `e12` is stale after a UI change:

```powershell
flaui-cli snapshot
flaui-cli click e18
```

## Daemon lifecycle

`flaui-cli` keeps a background daemon alive between invocations.

- The first stateful command starts it automatically.
- The daemon exits after 5 minutes of inactivity.
- After a timeout, run `launch` or `attach` again if there is no managed window, then take a new snapshot.

## Practical recovery flow

```powershell
flaui-cli list
flaui-cli attach "Calculator"
flaui-cli snapshot
flaui-cli click e7
```

If the window layout changed, repeat `snapshot` and continue with the new refs instead of reusing old ones.
