using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace FlaUI.Cli.Daemon;

internal static class DaemonPidFile
{
    private const string DirectoryName = "FlaUI.Cli";
    private const string FileName = "daemon.pid";
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
    private static readonly string PidFilePath = GetPath();

    public static Lease Acquire()
    {
        using var process = Process.GetCurrentProcess();
        var daemonInfo = new DaemonInfo(process.Id, process.StartTime);

        var directory = Path.GetDirectoryName(PidFilePath) ?? throw new InvalidOperationException("Cannot determine the daemon PID file directory.");
        Directory.CreateDirectory(directory);

        var payload = JsonSerializer.Serialize(daemonInfo);
        var bytes = Utf8NoBom.GetBytes(payload);
        using var stream = new FileStream(
            PidFilePath,
            new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Share = FileShare.Read,
            });
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush(flushToDisk: true);

        return new Lease();
    }

    public static async Task<DaemonInfo?> ReadIfExistsAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(PidFilePath))
        {
            return null;
        }

        await using var stream = new FileStream(
            PidFilePath,
            new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.ReadWrite,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
            });
        using var reader = new StreamReader(stream, Utf8NoBom, detectEncodingFromByteOrderMarks: false);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        return JsonSerializer.Deserialize<DaemonInfo>(payload)
            ?? throw new InvalidOperationException("Daemon PID file entry did not include daemon metadata.");
    }

    public static void DeleteIfExists()
    {
        if (!File.Exists(PidFilePath))
        {
            return;
        }

        File.Delete(PidFilePath);
    }

    internal sealed class Lease() : IDisposable
    {
        public void Dispose()
        {
            try
            {
                DeleteIfExists();
            }
            catch
            {
            }
        }
    }

    private static string GetPath()
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return string.IsNullOrWhiteSpace(localApplicationData)
            ? throw new InvalidOperationException("Cannot determine the LocalApplicationData directory for the current user.")
            : Path.Combine(localApplicationData, DirectoryName, FileName);
    }
}
