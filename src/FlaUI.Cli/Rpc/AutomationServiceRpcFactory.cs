using System.IO.Pipelines;
using Nerdbank.Streams;
using StreamJsonRpc;

namespace FlaUI.Cli.Rpc;

internal static class AutomationServiceRpcFactory
{
    private const string RpcChannelName = "IAutomationServiceChannel";

    public static async Task<IAutomationService> CreateClientAsync(
        MultiplexingStream multiplexingStream,
        CancellationToken cancellationToken = default)
    {
        var rpcChannel = await multiplexingStream.OfferChannelAsync(RpcChannelName, cancellationToken);
        return JsonRpc.Attach<IAutomationService>(CreateMessageHandler(rpcChannel, multiplexingStream));
    }

    public static async Task<JsonRpc> CreateServerAsync(
        MultiplexingStream multiplexingStream,
        IAutomationService rpcTarget,
        CancellationToken cancellationToken = default)
    {
        var rpcChannel = await multiplexingStream.AcceptChannelAsync(RpcChannelName, cancellationToken);
        return new JsonRpc(CreateMessageHandler(rpcChannel, multiplexingStream), rpcTarget);
    }

    private static LengthHeaderMessageHandler CreateMessageHandler(
        IDuplexPipe rpcChannel,
        MultiplexingStream multiplexingStream)
    {
        return new LengthHeaderMessageHandler(
            rpcChannel,
            new SystemTextJsonFormatter
            {
                MultiplexingStream = multiplexingStream,
            });
    }
}
