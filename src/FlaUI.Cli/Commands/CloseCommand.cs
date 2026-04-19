using System.CommandLine;
using FlaUI.Cli.Rpc;

namespace FlaUI.Cli.Commands;

internal sealed class CloseCommand : Command
{
    public CloseCommand()
        : base("close", "Close a window root from the latest snapshot.")
    {
        var refArgument = new Argument<string>("ref")
        {
            Description = "Root window ref from the latest snapshot.",
            DefaultValueFactory = _ => "e1",
        };
        Arguments.Add(refArgument);
        SetAction(async (parseResult, cancellationToken) =>
        {
            var targetRef = ElementRef.Parse(parseResult.GetRequiredValue(refArgument));
            return await CommandHelper.InvokeAutomationServiceAsync(
                async (proxy, ct) =>
                {
                    await proxy.CloseAsync(targetRef, ct);
                    Console.Out.WriteLine($"Closed {targetRef}.");

                    try
                    {
                        await CommandHelper.SnapshotAsync(proxy, ct);
                    }
                    catch
                    {
                        // Ignore snapshot failure since the window might have been closed successfully but the snapshot might fail due to the window being closed.
                    }
                },
                cancellationToken);
        });
    }
}
