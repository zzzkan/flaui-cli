using PolyType;
using StreamJsonRpc;

namespace FlaUI.Cli.Rpc;

[JsonRpcContract]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
internal partial interface IAutomationService
{
    Task<WindowInfo> LaunchAsync(string fileName, string? arguments, CancellationToken cancellationToken = default);

    Task<WindowInfo> AttachAsync(string title, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WindowInfo>> ListAsync(CancellationToken cancellationToken = default);

    Task SnapshotAsync(Stream artifactStream, CancellationToken cancellationToken = default);

    Task ScreenshotAsync(ElementRef target, Stream artifactStream, CancellationToken cancellationToken = default);

    Task ClickAsync(ElementRef target, string button, bool isDouble, CancellationToken cancellationToken = default);

    Task<string> GetTextAsync(ElementRef target, CancellationToken cancellationToken = default);

    Task FillAsync(ElementRef target, string value, CancellationToken cancellationToken = default);

    Task FocusAsync(ElementRef target, CancellationToken cancellationToken = default);

    Task CloseAsync(ElementRef target, CancellationToken cancellationToken = default);
}
