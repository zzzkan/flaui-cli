using System.CommandLine;
using FlaUI.Cli.Rpc;

namespace FlaUI.Cli.Commands;

internal sealed class ClickCommand : Command
{
    public ClickCommand()
        : base("click", "Click an element.")
    {
        var refArgument = new Argument<string>("ref")
        {
            Description = "Element ref from the latest snapshot.",
        };
        Arguments.Add(refArgument);
        var buttonOption = new Option<string>("--button")
        {
            Description = "Mouse button to use.",
            DefaultValueFactory = _ => "left",
        };
        buttonOption.AcceptOnlyFromAmong("left", "right");
        Options.Add(buttonOption);
        var doubleOption = new Option<bool>("--double")
        {
            Description = "Double-click the element.",
        };
        Options.Add(doubleOption);
        SetAction(async (parseResult, cancellationToken) =>
        {
            var targetRef = ElementRef.Parse(parseResult.GetRequiredValue(refArgument));
            var button = parseResult.GetRequiredValue(buttonOption);
            var isDouble = parseResult.GetValue(doubleOption);
            return await CommandHelper.InvokeAutomationServiceAsync(
                async (proxy, ct) =>
                {
                    await proxy.ClickAsync(targetRef, button, isDouble, ct);
                    var message = $"Clicked {targetRef} with {button} button{(isDouble ? " (double-click)" : "")}.";
                    Console.Out.WriteLine(message);

                    await CommandHelper.SnapshotAsync(proxy, ct);
                    return 0;
                },
                cancellationToken);
        });
    }
}
