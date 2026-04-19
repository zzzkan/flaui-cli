using System.CommandLine;

namespace FlaUI.Cli.Commands;

internal sealed class SnapshotCommand : Command
{
    public SnapshotCommand()
        : base("snapshot", "Get the accessibility tree for the attached window with refs.")
    {
        SetAction(async (parseResult, cancellationToken) =>
        {
            return await CommandHelper.InvokeAutomationServiceAsync(
                async (proxy, ct) =>
                {
                    await CommandHelper.SnapshotAsync(proxy, ct);
                },
                cancellationToken);
        });
    }
}
