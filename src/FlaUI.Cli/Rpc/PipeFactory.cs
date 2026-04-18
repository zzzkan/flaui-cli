using System.IO.Pipes;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace FlaUI.Cli.Rpc;

internal static class PipeFactory
{
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(5);
    private static readonly string PipeName = GetName();

    public static NamedPipeServerStream CreateServer()
    {
        return new NamedPipeServerStream(
            PipeName,
            PipeDirection.InOut,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly | PipeOptions.FirstPipeInstance);
    }

    public static async Task<NamedPipeClientStream> CreateClientAndConnectAsync(CancellationToken cancellationToken = default)
    {
        var pipe = new NamedPipeClientStream(
            ".",
            PipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);
        await pipe.ConnectAsync(ConnectionTimeout, cancellationToken);
        return pipe;
    }

    private static string GetName()
    {
        var userSid = WindowsIdentity.GetCurrent().User?.Value
            ?? throw new InvalidOperationException("Cannot determine the current Windows user SID.");
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(userSid));
        return $"flaui-cli-{Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant()}";
    }
}
