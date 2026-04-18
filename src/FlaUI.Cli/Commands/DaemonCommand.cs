using System.CommandLine;
using FlaUI.Cli.Daemon;

namespace FlaUI.Cli.Commands;

internal sealed class DaemonCommand : Command
{
    public DaemonCommand()
        : base("daemon", "Internal daemon management commands.")
    {
        Hidden = true;
        var runCommand = new Command("run", "Run the background daemon.")
        {
            Hidden = true,
        };
        runCommand.SetAction(async (parseResult) =>
        {
            await DaemonProcessManager.StartAsync(TimeSpan.FromMinutes(5), CancellationToken.None);
            return 0;
        });

        var stopCommand = new Command("stop", "Stop the background daemon.")
        {
            Hidden = true,
        };
        stopCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var daemonInfo = await DaemonProcessManager.StopAsync(cancellationToken);
            Console.Out.WriteLine("### Stopped Daemon Process");
            Console.Out.WriteLine($"- PID: {daemonInfo.ProcessId}");
            Console.Out.WriteLine($"- Start Time: {daemonInfo.ProcessStartTime}");
            return 0;
        });

        Subcommands.Add(runCommand);
        Subcommands.Add(stopCommand);
    }
}
