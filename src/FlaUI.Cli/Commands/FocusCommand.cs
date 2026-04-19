using System.CommandLine;
using FlaUI.Cli.Rpc;

namespace FlaUI.Cli.Commands;

internal sealed class FocusCommand : Command
{
    public FocusCommand()
        : base("focus", "Bring a window root from the latest snapshot to the foreground.")
    {
        var refArgument = new Argument<string>("ref")
        {
            Description = "Root window ref from the latest snapshot.",
        };

        Arguments.Add(refArgument);
        SetAction(async (parseResult, cancellationToken) =>
        {
            var windowRef = ElementRef.Parse(parseResult.GetRequiredValue(refArgument));
            return await CommandHelper.InvokeAutomationServiceWithSnapshotAsync(
                async (proxy, ct) =>
                {
                    await proxy.FocusAsync(windowRef, ct);
                    Console.Out.WriteLine($"Focused {windowRef}.");
                },
                cancellationToken);
        });
    }
}
