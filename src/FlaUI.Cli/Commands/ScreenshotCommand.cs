using System.CommandLine;
using FlaUI.Cli.Rpc;
namespace FlaUI.Cli.Commands;

internal sealed class ScreenshotCommand : Command
{
    public ScreenshotCommand()
        : base("screenshot", "Take a screenshot.")
    {
        var refArgument = new Argument<string>("ref")
        {
            Description = "Element or window ref to capture.",
        };
        Arguments.Add(refArgument);
        SetAction(async (parseResult, cancellationToken) =>
        {
            var refId = parseResult.GetRequiredValue(refArgument);
            return await CommandHelper.InvokeAutomationServiceAsync(
                async (proxy, ct) =>
                {
                    await CommandHelper.ScreenshotAsync(ElementRef.Parse(refId), proxy, ct);
                },
                cancellationToken);
        });
    }
}
