using System.Diagnostics;
using FlaUI.Cli.Core;
using FlaUI.Cli.Rpc;

namespace FlaUI.Cli.Daemon;

internal static class DaemonProcessManager
{
    public static async Task StartAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var pidFileLease = DaemonPidFile.Acquire();
        using var rpcTarget = new AutomationService();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        while (true)
        {
            using var pipe = PipeFactory.CreateServer();

            cts.Token.ThrowIfCancellationRequested();
            Console.Out.WriteLine("READY");
            Console.Out.Flush();

            await pipe.WaitForConnectionAsync(cts.Token);
            using var rpc = AutomationServiceRpcFactory.CreateServer(pipe, rpcTarget);

            cts.Token.ThrowIfCancellationRequested();
            Console.Out.WriteLine("CLIENT_CONNECTED");

            rpc.StartListening();
            await rpc.Completion.WaitAsync(cts.Token);

            cts.Token.ThrowIfCancellationRequested();
            Console.Out.WriteLine("CLIENT_DISCONNECTED");

            cts.CancelAfter(timeout);
        }
    }

    public static async Task<DaemonInfo> StopAsync(CancellationToken cancellationToken = default)
    {
        var daemonInfo = await DaemonPidFile.ReadIfExistsAsync(cancellationToken)
                ?? throw new InvalidOperationException("Daemon is not running.");
        try
        {
            using var process = Process.GetProcessById(daemonInfo.ProcessId);
            process.Kill();
            await process.WaitForExitAsync(cancellationToken);
            return daemonInfo;
        }
        catch
        {
            throw new InvalidOperationException("Failed to stop the daemon process. It may have already exited, or the PID file may be stale. Cleaned up the PID file.");
        }
        finally
        {
            DaemonPidFile.DeleteIfExists();
        }
    }

    public static async Task<DaemonInfo?> EnsureExternalProcessRunningAsync(CancellationToken cancellationToken = default)
    {
        if (await DaemonPidFile.ReadIfExistsAsync(cancellationToken) is { } daemonInfo)
        {
            try
            {
                using var existingProcess = Process.GetProcessById(daemonInfo.ProcessId);
                return null; // Daemon is already running, no need to start a new one.
            }
            catch
            {
                DaemonPidFile.DeleteIfExists();
                throw new InvalidOperationException("Daemon is not running but PID file exists. Cleaned up the PID file.");
            }
        }

        var processPath = Environment.ProcessPath ?? string.Empty;
        var assemblyLocation = typeof(DaemonProcessManager).Assembly.Location;
        var isDotNet = Path.GetFileNameWithoutExtension(processPath)
                .Equals("dotnet", StringComparison.OrdinalIgnoreCase);
        var startInfo = new ProcessStartInfo
        {
            FileName = isDotNet ? "dotnet" : processPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        if (isDotNet)
        {
            startInfo.ArgumentList.Add(assemblyLocation);
        }
        startInfo.ArgumentList.Add("daemon");
        startInfo.ArgumentList.Add("run");

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the daemon process.");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(30));
        var line = await process.StandardOutput.ReadLineAsync(cts.Token);
        if (!string.Equals(line, "READY", StringComparison.Ordinal))
        {
            process.Kill();
            await process.WaitForExitAsync(cancellationToken);
            throw new InvalidOperationException("Daemon process did not signal readiness.");
        }

        daemonInfo = await DaemonPidFile.ReadIfExistsAsync(cancellationToken);
        if (daemonInfo is null)
        {
            process.Kill();
            await process.WaitForExitAsync(cancellationToken);
            throw new InvalidOperationException("Daemon process did not create a valid PID file.");
        }

        return daemonInfo;
    }
}
