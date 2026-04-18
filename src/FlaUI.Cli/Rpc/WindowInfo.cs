namespace FlaUI.Cli.Rpc;

internal sealed record WindowInfo
{
    public WindowInfo(string title, string processName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(processName);

        Title = title;
        ProcessName = processName;
    }

    public string Title { get; }

    public string ProcessName { get; }
}
