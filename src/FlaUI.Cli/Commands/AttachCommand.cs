using System.CommandLine;

namespace FlaUI.Cli.Commands;

internal sealed class AttachCommand : Command
{
    public AttachCommand()
        : base("attach", "Attach a visible top-level desktop window as the managed window.")
    {
        var titleArgument = new Argument<string>("title")
        {
            Description = "Visible top-level desktop window title substring.",
        };
        Arguments.Add(titleArgument);
        Validators.Add(result =>
        {
            var title = result.GetValue(titleArgument);
            if (title is not null && string.IsNullOrWhiteSpace(title))
            {
                result.AddError("Title cannot be empty.");
            }
        });
        SetAction(async (parseResult, cancellationToken) =>
        {
            var title = parseResult.GetRequiredValue(titleArgument);
            return await CommandHelper.InvokeAutomationServiceWithSnapshotAsync(
                async (proxy, ct) =>
                {
                    var window = await proxy.AttachAsync(title, ct);
                    Console.Out.WriteLine("### Attached Window");
                    Console.Out.WriteLine($"- Title: {window.Title}");
                    Console.Out.WriteLine($"- Process Name: {window.ProcessName}");
                },
                cancellationToken);
        });
    }
}
