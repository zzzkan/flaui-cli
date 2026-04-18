using System.CommandLine;

namespace FlaUI.Cli.Commands;

internal sealed class LaunchCommand : Command
{
    public LaunchCommand()
        : base("launch", "Launch an application and attach its first visible top-level window.")
    {
        var filenameArgument = new Argument<string>("filename")
        {
            Description = "Application path, executable name.",
        };
        var appArgsOption = new Option<string?>("--args")
        {
            Description = "Arguments passed to the application.",
        };

        Arguments.Add(filenameArgument);
        Options.Add(appArgsOption);
        SetAction(async (parseResult, cancellationToken) =>
        {
            var filename = parseResult.GetRequiredValue(filenameArgument);
            var args = parseResult.GetValue(appArgsOption);

            return await CommandHelper.InvokeAutomationServiceAsync(
                async (proxy, ct) =>
                {
                    var window = await proxy.LaunchAsync(filename, string.IsNullOrWhiteSpace(args) ? null : args, ct);
                    Console.Out.WriteLine("### Attached Window");
                    Console.Out.WriteLine($"- Title: {window.Title}");
                    Console.Out.WriteLine($"- Process Name: {window.ProcessName}");

                    await CommandHelper.SnapshotAsync(proxy, ct);
                    return 0;
                },
                cancellationToken);
        });
    }
}
