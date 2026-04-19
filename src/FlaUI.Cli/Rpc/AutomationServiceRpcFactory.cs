using StreamJsonRpc;

namespace FlaUI.Cli.Rpc;

internal static class AutomationServiceRpcFactory
{
    public static IAutomationService CreateClient(Stream pipe)
    {
        return JsonRpc.Attach<IAutomationService>(CreateMessageHandler(pipe));
    }

    public static JsonRpc CreateServer(
        Stream pipe,
        IAutomationService rpcTarget)
    {
        return new JsonRpc(CreateMessageHandler(pipe), rpcTarget);
    }

    private static LengthHeaderMessageHandler CreateMessageHandler(
        Stream pipe)
    {
        return new LengthHeaderMessageHandler(pipe, pipe, new MessagePackFormatter());
    }
}
