# Artifacts and installation

Use this reference when `flaui-cli` is not installed yet or when you need to explain where snapshots and screenshots are written.

## Requirements

- Windows x64
- .NET 10 SDK or newer for install, update, and uninstall operations

## Global tool installation

```powershell
dotnet tool install --global flaui-cli
flaui-cli --help
```

## Local tool installation

Use a local tool manifest when you do not want a global install.

```powershell
dotnet new tool-manifest
dotnet tool install flaui-cli
dotnet tool run flaui-cli --help
```

## Local package validation from this repository

```powershell
dotnet pack .\src\FlaUI.Cli\FlaUI.Cli.csproj -c Release
dotnet tool install --global --add-source .\artifacts flaui-cli --version 0.1.0
```

## Artifact layout

`flaui-cli` writes artifacts under `.flaui-cli/` in the current working directory.

- `snapshot-<timestamp>.yml`
- `screenshot-<timestamp>.png`

The working directory matters. If you want artifacts in a specific project folder, run `flaui-cli` from that folder.
