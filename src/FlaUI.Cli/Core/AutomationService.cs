using System.Diagnostics;
using System.Drawing.Imaging;
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
    private readonly UIA3Automation _automation = new();
    private Window? _attachedWindow;
    private Dictionary<ElementRef, AutomationElement> _activeElements = [];

    public Task<WindowInfo> LaunchAsync(
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
        SetAttachedWindow(application.GetMainWindow(_automation, TimeSpan.FromSeconds(30)));
        if (_attachedWindow is null)
        {
            throw new InvalidOperationException($"Failed to find main window of the launched application '{fileName}'.");
        }
        return Task.FromResult(_attachedWindow.ToWindowInfo());
    }

    public Task<WindowInfo> AttachAsync(
        string title,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        cancellationToken.ThrowIfCancellationRequested();

        var firstWindow = _automation.GetDesktop()
                .FindAllChildren(cf => cf.ByControlType(ControlType.Window))
                .Select(child => child.AsWindow())
                .FirstOrDefault(window => window.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
        SetAttachedWindow(firstWindow);
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

    public async Task SnapshotAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        cancellationToken.ThrowIfCancellationRequested();

        var attachedWindow = RequireAttachedWindow();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false), bufferSize: 4096, leaveOpen: true);
        _activeElements = SnapshotBuilder.Build(attachedWindow, writer);
        await writer.FlushAsync(cancellationToken);
    }

    public async Task ScreenshotAsync(
        ElementRef target,
        Stream stream,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(stream);
        cancellationToken.ThrowIfCancellationRequested();

        var element = RequireElement(target);
        element.Focus();
        using var capture = Capture.Element(element);
        capture.Bitmap.Save(stream, ImageFormat.Png);
        await stream.FlushAsync(cancellationToken);
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
