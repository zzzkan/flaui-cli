using FlaUI.Core.AutomationElements;

namespace FlaUI.Cli.Core;

internal sealed class SnapshotNode(
    AutomationElement element,
    IReadOnlyList<SnapshotNode> children)
{
    public AutomationElement Element { get; } = element;

    public string Role { get; } = element.SnapshotControlType.ToString().ToLowerInvariant();

    public string? Name { get; } = element.SnapshotName?.ReplaceLineEndings(string.Empty);

    public string? Value { get; } = element.SnapshotValue?.ReplaceLineEndings(string.Empty);

    public IReadOnlyList<string> States { get; } = element.SnapshotStates;

    public IReadOnlyList<SnapshotNode> Children { get; } = children;
}
