namespace FlaUI.Cli.Daemon;

internal sealed record DaemonInfo(
    int ProcessId,
    DateTime ProcessStartTime);
