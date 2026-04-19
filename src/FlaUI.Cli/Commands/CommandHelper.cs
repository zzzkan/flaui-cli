using System.Globalization;
using FlaUI.Cli.Daemon;
using FlaUI.Cli.Rpc;

namespace FlaUI.Cli.Commands;

internal static class CommandHelper
{
    public static async Task<int> InvokeAutomationServiceAsync(
        Func<IAutomationService, CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        if (await DaemonProcessManager.EnsureExternalProcessRunningAsync(cancellationToken) is { } daemon)
        {
            Console.Out.WriteLine("### Started Daemon Process");
            Console.Out.WriteLine($"- PID: {daemon.ProcessId}");
            Console.Out.WriteLine($"- Start Time: {daemon.ProcessStartTime}");
        }

        await using var pipe = await PipeFactory.CreateClientAndConnectAsync(cancellationToken);
        var proxy = AutomationServiceRpcFactory.CreateClient(pipe);
        await action(proxy, cancellationToken);
        return 0;
    }

    public static Task<int> InvokeAutomationServiceWithSnapshotAsync(
        Func<IAutomationService, CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        return InvokeAutomationServiceAsync(
            async (proxy, ct) =>
            {
                await action(proxy, ct);
                await SnapshotAsync(proxy, ct);
            },
            cancellationToken);
    }

    public static async Task SnapshotAsync(
        IAutomationService proxy,
        CancellationToken cancellationToken = default)
    {
        var (_, displayPath) = await WriteFileAsync(
            $"snapshot-{Timestamp}.yml",
            proxy.SnapshotAsync,
            cancellationToken);
        Console.Out.WriteLine("### Snapshot");
        Console.Out.WriteLine($"[Snapshot]({displayPath})");
    }

    public static async Task ScreenshotAsync(
        ElementRef target,
        IAutomationService proxy,
        CancellationToken cancellationToken = default)
    {
        var (_, displayPath) = await WriteFileAsync(
            $"screenshot-{Timestamp}.png",
            (path, ct) => proxy.ScreenshotAsync(target, path, ct),
            cancellationToken);
        Console.Out.WriteLine("### Screenshot");
        Console.Out.WriteLine($"[Screenshot]({displayPath})");
    }

    private static string Timestamp => DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss-fffZ", CultureInfo.InvariantCulture);

    private static async Task<(string FullPath, string DisplayPath)> WriteFileAsync(
        string fileName,
        Func<string, CancellationToken, Task> writeAsync,
        CancellationToken cancellationToken = default)
    {
        const string subDirectory = ".flaui-cli";
        var directory = Path.Combine(Directory.GetCurrentDirectory(), subDirectory);
        Directory.CreateDirectory(directory);
        var fullPath = Path.GetFullPath(Path.Combine(directory, fileName));
        var displayPath = $"{subDirectory}/{fileName}";
        try
        {
            await writeAsync(fullPath, cancellationToken);
            return (fullPath, displayPath);
        }
        catch
        {
            TryDeleteFile(fullPath);
            throw;
        }
    }

    private static void TryDeleteFile(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch
        {
        }
    }
}
