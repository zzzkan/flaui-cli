using MessagePack;

namespace FlaUI.Cli.Rpc;

[MessagePackObject]
internal sealed record WindowInfo
{
    public WindowInfo(string title, string processName)
    {
        ArgumentNullException.ThrowIfNull(title, nameof(title));
        ArgumentNullException.ThrowIfNull(processName, nameof(processName));

        Title = title;
        ProcessName = processName;
    }

    [Key(0)]
    public string Title { get; }

    [Key(1)]
    public string ProcessName { get; }
}
