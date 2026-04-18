using System.CommandLine;
using FlaUI.Cli.Rpc;

namespace FlaUI.Cli.Commands;

internal sealed class GetTextCommand : Command
{
    public GetTextCommand()
        : base("get-text", "Get text content of an element.")
    {
        var refArgument = new Argument<string>("ref")
        {
            Description = "Element ref to read from.",
        };

        Arguments.Add(refArgument);
        SetAction(async (parseResult, cancellationToken) =>
        {
            var targetRef = ElementRef.Parse(parseResult.GetRequiredValue(refArgument));
            return await CommandHelper.InvokeAutomationServiceAsync(
                async (proxy, ct) =>
                {
                    var text = await proxy.GetTextAsync(targetRef, ct);
                    Console.Out.WriteLine($"### Text of {targetRef}");

                    var width = Console.WindowWidth - 2;
                    for (var i = 0; i < text.Length; i += width)
                    {
                        var length = Math.Min(width, text.Length - i);
                        Console.Out.WriteLine(text.AsSpan(i, length));
                    }
                    return 0;
                },
                cancellationToken);
        });
    }
}
