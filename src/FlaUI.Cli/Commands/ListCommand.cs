using System.CommandLine;

namespace FlaUI.Cli.Commands;

internal sealed class ListCommand : Command
{
    public ListCommand()
        : base("list", "List visible top-level desktop windows.")
    {
        SetAction(async (parseResult, cancellationToken) =>
        {
            await CommandHelper.InvokeAutomationServiceAsync(
                async (proxy, ct) =>
                {
                    var windows = await proxy.ListAsync(ct);
                    if (windows.Count == 0)
                    {
                        Console.Out.WriteLine("No visible top-level desktop windows found.");
                        return 0;
                    }

                    Console.Out.WriteLine("### Windows");
                    foreach (var window in windows)
                    {
                        Console.Out.WriteLine($"- {window.Title} ({window.ProcessName})");
                    }

                    return 0;
                },
                cancellationToken);
        });
    }
}
