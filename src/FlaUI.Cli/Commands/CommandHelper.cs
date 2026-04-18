using System.Globalization;
using FlaUI.Cli.Daemon;
using FlaUI.Cli.Rpc;
using Nerdbank.Streams;

namespace FlaUI.Cli.Commands;

internal static class CommandHelper
{
    public static async Task<TResult> InvokeAutomationServiceAsync<TResult>(
        Func<IAutomationService, CancellationToken, Task<TResult>> action,
        CancellationToken cancellationToken = default)
    {
        if (await DaemonProcessManager.EnsureExternalProcessRunningAsync(cancellationToken) is { } daemon)
        {
            Console.Out.WriteLine("### Started Daemon Process");
            Console.Out.WriteLine($"- PID: {daemon.ProcessId}");
            Console.Out.WriteLine($"- Start Time: {daemon.ProcessStartTime}");
        }

        await using var pipe = await PipeFactory.CreateClientAndConnectAsync(cancellationToken);
        await using var stream = await MultiplexingStream.CreateAsync(pipe, cancellationToken);
        var proxy = await AutomationServiceRpcFactory.CreateClientAsync(stream, cancellationToken);
        return await action(proxy, cancellationToken);
    }

    public static async Task SnapshotAsync(
        IAutomationService automation,
        CancellationToken cancellationToken = default)
    {
        var fileName = $"snapshot-{Timestamp}.yml";
        var (_, displayPath) = await WriteFileStream(
            fileName,
            automation.SnapshotAsync,
            cancellationToken);
        Console.Out.WriteLine("### Snapshot");
        Console.Out.WriteLine($"[Snapshot]({displayPath})");
    }

    public static async Task ScreenshotAsync(
        ElementRef target,
        IAutomationService automation,
        CancellationToken cancellationToken = default)
    {
        var fileName = $"screenshot-{Timestamp}.png";
        var (_, displayPath) = await WriteFileStream(
            fileName,
            (stream, ct) => automation.ScreenshotAsync(target, stream, ct),
            cancellationToken);
        Console.Out.WriteLine("### Screenshot");
        Console.Out.WriteLine($"[Screenshot]({displayPath})");
    }

    private static string Timestamp => DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss-fffZ", CultureInfo.InvariantCulture);

    private static async Task<(string FullPath, string DisplayPath)> WriteFileStream(
        string fileName,
        Func<Stream, CancellationToken, Task> writeAsync,
        CancellationToken cancellationToken = default)
    {
        const string subDirectory = ".flaui-cli";
        var directory = Path.Combine(Directory.GetCurrentDirectory(), subDirectory);
        Directory.CreateDirectory(directory);
        var fullPath = Path.GetFullPath(Path.Combine(directory, fileName));
        var displayPath = $"{subDirectory}/{fileName}";
        try
        {
            await using var stream = new FileStream(
                fullPath,
                new FileStreamOptions
                {
                    Mode = FileMode.CreateNew,
                    Access = FileAccess.Write,
                    Share = FileShare.Read,
                    Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
                });
            await writeAsync(stream, cancellationToken);
            await stream.FlushAsync(cancellationToken);
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
