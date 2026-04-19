using System.CommandLine;
using FlaUI.Cli.Rpc;

namespace FlaUI.Cli.Commands;

internal sealed class FillCommand : Command
{
    public FillCommand()
        : base("fill", "Clear and fill a text field.")
    {
        var refArgument = new Argument<string>("ref")
        {
            Description = "Element ref to fill.",
        };
        var valueArgument = new Argument<string>("value")
        {
            Description = "Replacement value.",
        };

        Arguments.Add(refArgument);
        Arguments.Add(valueArgument);
        SetAction(async (parseResult, cancellationToken) =>
        {
            var targetRef = ElementRef.Parse(parseResult.GetRequiredValue(refArgument));
            var value = parseResult.GetRequiredValue(valueArgument);
            return await CommandHelper.InvokeAutomationServiceWithSnapshotAsync(
                async (proxy, ct) =>
                {
                    await proxy.FillAsync(targetRef, value, ct);
                    Console.Out.WriteLine($"Filled {targetRef} with value '{value}'.");
                },
                cancellationToken);
        });
    }
}
