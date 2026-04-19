using System.CommandLine;
using System.Runtime.InteropServices;
using FlaUI.Cli.Commands;
using StreamJsonRpc;
using StreamJsonRpc.Protocol;

if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    throw new PlatformNotSupportedException("This CLI tool only supports Windows.");
}

var rootCommand = new RootCommand("Windows application automation CLI tool using FlaUI.");

rootCommand.Subcommands.Add(new LaunchCommand());
rootCommand.Subcommands.Add(new AttachCommand());
rootCommand.Subcommands.Add(new ListCommand());
rootCommand.Subcommands.Add(new SnapshotCommand());
rootCommand.Subcommands.Add(new ClickCommand());
rootCommand.Subcommands.Add(new FillCommand());
rootCommand.Subcommands.Add(new GetTextCommand());
rootCommand.Subcommands.Add(new FocusCommand());
rootCommand.Subcommands.Add(new CloseCommand());
rootCommand.Subcommands.Add(new ScreenshotCommand());
rootCommand.Subcommands.Add(new DaemonCommand());

var debugOption = new Option<bool>("--debug")
{
    Description = "Enable detailed exception messages for debugging.",
    Hidden = true,
};
rootCommand.Options.Add(debugOption);

try
{
    var parseResult = rootCommand.Parse(args);
    var configuration = new InvocationConfiguration
    {
        EnableDefaultExceptionHandler = parseResult.GetValue(debugOption),
    };

    return await parseResult.InvokeAsync(configuration);
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine(GetMessage(ex));
    return 1;
}


static string GetMessage(Exception exception)
{
    return exception switch
    {
        RemoteInvocationException remoteInvocationException => GetRemoteMessage(remoteInvocationException),
        AggregateException { InnerExceptions.Count: 1 } aggregateException => GetMessage(aggregateException.InnerExceptions[0]),
        _ when !string.IsNullOrWhiteSpace(exception.Message) => exception.Message,
        _ => exception.GetType().Name,
    };
}

static string GetRemoteMessage(RemoteInvocationException exception)
{
    if (exception.DeserializedErrorData is CommonErrorData errorData
        && !string.IsNullOrWhiteSpace(errorData.Message))
    {
        return errorData.Message;
    }

    if (exception.InnerException is not null
        && !string.IsNullOrWhiteSpace(exception.InnerException.Message))
    {
        return exception.InnerException.Message;
    }

    return !string.IsNullOrWhiteSpace(exception.Message)
        ? exception.Message
        : "The remote operation failed.";
}
