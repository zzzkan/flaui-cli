using System.Diagnostics;
using System.Text;
using FlaUI.Cli.Rpc;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

namespace FlaUI.Cli.Core;

internal sealed class AutomationService : IAutomationService, IDisposable
{
    private const int SnapshotFileBufferSize = 64 * 1024;
    private const int SnapshotWriterBufferSize = 32 * 1024;
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly UIA3Automation _automation = new();
    private Window? _attachedWindow;
    private Dictionary<ElementRef, AutomationElement> _activeElements = [];

    public async Task<WindowInfo> LaunchAsync(
        string fileName,
        string? arguments,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        cancellationToken.ThrowIfCancellationRequested();

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            Arguments = arguments ?? string.Empty
        };
        using var application = Application.Launch(startInfo);
        application.WaitWhileBusy(TimeSpan.FromSeconds(1));

        Window? window = null;

        try
        {
            window = application.GetMainWindow(_automation, TimeSpan.FromSeconds(1));
            window ??= application.GetAllTopLevelWindows(_automation).FirstOrDefault();
        }
        catch
        {
            // Ignore exceptions from FlaUI and fallback to searching windows by process ID.
        }

        // fallback: search for windows belonging to the process ID, in case the launched app doesn't expose a main window or FlaUI fails to find it.
        if (window is null)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken); // Wait a moment for the window to appear.
            var name = Path.GetFileNameWithoutExtension(fileName);
            window = _automation.GetDesktop()
            .FindAllChildren(cf => cf.ByControlType(ControlType.Window))
            .Select(child => child.AsWindow())
            .FirstOrDefault(
                w => w.ToWindowInfo().ProcessName.Contains(name, StringComparison.OrdinalIgnoreCase)
            );
        }

        SetAttachedWindow(window);
        if (_attachedWindow is null)
        {
            throw new InvalidOperationException($"Failed to find any window for the launched application '{fileName}'.");
        }
        return _attachedWindow.ToWindowInfo();
    }

    public Task<WindowInfo> AttachAsync(
        string title,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        cancellationToken.ThrowIfCancellationRequested();

        var window = _automation.GetDesktop()
                .FindAllChildren(cf => cf.ByControlType(ControlType.Window))
                .Select(child => child.AsWindow())
                .FirstOrDefault(window => window.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
        SetAttachedWindow(window);
        if (_attachedWindow is null)
        {
            throw new InvalidOperationException($"Failed to find window with title containing '{title}'.");
        }
        return Task.FromResult(_attachedWindow.ToWindowInfo());
    }

    public Task<IReadOnlyList<WindowInfo>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var windowInfos = _automation
            .GetDesktop()
            .FindAllChildren(cf => cf.ByControlType(ControlType.Window))
            .Select(child => child.AsWindow().ToWindowInfo())
            .ToArray();
        return Task.FromResult<IReadOnlyList<WindowInfo>>(windowInfos);
    }

    public Task SnapshotAsync(
        string path,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(path);
        cancellationToken.ThrowIfCancellationRequested();

        var attachedWindow = RequireAttachedWindow();
        using var stream = new FileStream(
                path,
                new FileStreamOptions
                {
                    Mode = FileMode.CreateNew,
                    Access = FileAccess.Write,
                    Share = FileShare.Read,
                    BufferSize = SnapshotFileBufferSize,
                });
        using var writer = new StreamWriter(stream, Utf8NoBom, bufferSize: SnapshotWriterBufferSize);
        _activeElements = SnapshotBuilder.Build(attachedWindow, writer);
        return Task.CompletedTask;
    }

    public Task ScreenshotAsync(
        ElementRef target,
        string path,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(path);
        cancellationToken.ThrowIfCancellationRequested();

        var element = RequireElement(target);
        element.Focus();
        using var capture = Capture.Element(element);
        capture.ToFile(path);
        return Task.CompletedTask;
    }

    public Task ClickAsync(
        ElementRef target,
        string button,
        bool isDouble,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);
        cancellationToken.ThrowIfCancellationRequested();

        var element = RequireElement(target);
        switch ((button.ToLowerInvariant(), isDouble))
        {
            case ("left", false):
                element.Click();
                break;
            case ("left", true):
                element.DoubleClick();
                break;
            case ("right", false):
                element.RightClick();
                break;
            case ("right", true):
                element.RightDoubleClick();
                break;
            default:
                throw new ArgumentException($"Invalid click options: button={button}, double={isDouble}");
        }
        return Task.CompletedTask;
    }

    public Task FillAsync(
        ElementRef target,
        string value,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(value);
        cancellationToken.ThrowIfCancellationRequested();

        var element = RequireElement(target);
        if (element.Patterns.Value.IsSupported)
        {
            element.Patterns.Value.Pattern.SetValue(value);
            return Task.CompletedTask;
        }
        throw new InvalidOperationException($"Element {target} does not support value pattern.");
    }

    public Task<string> GetTextAsync(
        ElementRef target,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);
        cancellationToken.ThrowIfCancellationRequested();

        var element = RequireElement(target);
        if (element.Patterns.Value.IsSupported)
        {
            var text = element.Patterns.Value.Pattern.Value.ValueOrDefault;
            if (!string.IsNullOrEmpty(text))
            {
                return Task.FromResult(text);
            }
        }
        if (element.Patterns.Text.IsSupported)
        {
            var text = element.Patterns.Text.Pattern.DocumentRange.GetText(-1);
            if (!string.IsNullOrEmpty(text))
            {
                return Task.FromResult(text);
            }
        }
        var name = element.Properties.Name.ValueOrDefault ?? string.Empty;
        return Task.FromResult(name);
    }

    public Task FocusAsync(
        ElementRef target,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);
        cancellationToken.ThrowIfCancellationRequested();

        var element = RequireElement(target);
        element.Focus();
        return Task.CompletedTask;
    }

    public Task CloseAsync(
        ElementRef target,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);
        cancellationToken.ThrowIfCancellationRequested();

        var window = RequireElement(target).AsWindow() ?? throw new InvalidOperationException($"Element {target} is not a window and cannot be closed.");
        window.Close();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _automation.Dispose();
    }

    private void SetAttachedWindow(Window? window)
    {
        _attachedWindow = window;
        _activeElements.Clear();
    }

    private void ClearAttachedState()
    {
        SetAttachedWindow(null);
    }

    private Window RequireAttachedWindow()
    {
        var attachedWindow = _attachedWindow
            ?? throw new InvalidOperationException("No window attached. Use 'attach' or 'launch' command first.");

        try
        {
            _ = attachedWindow.Title;
            return attachedWindow;
        }
        catch (Exception ex)
        {
            ClearAttachedState();
            throw new InvalidOperationException("Attached window is no longer available. Use 'attach' or 'launch' command first.", ex);
        }
    }

    private AutomationElement RequireElement(ElementRef targetRef)
    {
        _ = RequireAttachedWindow();

        var element = _activeElements.GetValueOrDefault(targetRef)
            ?? throw new InvalidOperationException($"Element not found: {targetRef}. Run 'snapshot' to refresh refs.");

        try
        {
            _ = element.Properties.ControlType.ValueOrDefault;
            return element;
        }
        catch (Exception ex)
        {
            ClearAttachedState();
            throw new InvalidOperationException($"Element {targetRef} is no longer available. Use 'attach' or 'launch' command first.", ex);
        }
    }
}
