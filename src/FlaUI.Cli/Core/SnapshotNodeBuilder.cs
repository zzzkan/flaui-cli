using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace FlaUI.Cli.Core;

internal static class SnapshotNodeBuilder
{
    public static SnapshotNode Build(Window window, int maxDepth = 10)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentOutOfRangeException.ThrowIfNegative(maxDepth);

        using var activation = CreateCacheRequest(window.Automation).Activate();

        var walker = window.Automation.TreeWalkerFactory.GetControlViewWalker();
        var root = window.FrameworkAutomationElement.GetUpdatedCache() ?? window;
        var rootNodes = BuildNodes(root, 0, maxDepth, walker, forceInclude: true);

        return rootNodes.Count != 1 ? throw new InvalidOperationException("Snapshot must contain exactly one root node.") : rootNodes[0];
    }

    private static CacheRequest CreateCacheRequest(AutomationBase automation)
    {
        var cacheRequest = new CacheRequest
        {
            AutomationElementMode = AutomationElementMode.Full,
            TreeScope = TreeScope.Element,
        };
        cacheRequest.Add(automation.PropertyLibrary.Element.AutomationId);
        cacheRequest.Add(automation.PropertyLibrary.Element.BoundingRectangle);
        cacheRequest.Add(automation.PropertyLibrary.Element.ControlType);
        cacheRequest.Add(automation.PropertyLibrary.Element.IsEnabled);
        cacheRequest.Add(automation.PropertyLibrary.Element.IsKeyboardFocusable);
        cacheRequest.Add(automation.PropertyLibrary.Element.IsOffscreen);
        cacheRequest.Add(automation.PropertyLibrary.Element.Name);
        cacheRequest.Add(automation.PropertyLibrary.ExpandCollapse.ExpandCollapseState);
        cacheRequest.Add(automation.PropertyLibrary.RangeValue.IsReadOnly);
        cacheRequest.Add(automation.PropertyLibrary.RangeValue.Value);
        cacheRequest.Add(automation.PropertyLibrary.SelectionItem.IsSelected);
        cacheRequest.Add(automation.PropertyLibrary.Toggle.ToggleState);
        cacheRequest.Add(automation.PropertyLibrary.Value.IsReadOnly);
        cacheRequest.Add(automation.PropertyLibrary.Value.Value);
        cacheRequest.Add(automation.PatternLibrary.ExpandCollapsePattern);
        cacheRequest.Add(automation.PatternLibrary.InvokePattern);
        cacheRequest.Add(automation.PatternLibrary.RangeValuePattern);
        cacheRequest.Add(automation.PatternLibrary.SelectionItemPattern);
        cacheRequest.Add(automation.PatternLibrary.TogglePattern);
        cacheRequest.Add(automation.PatternLibrary.ValuePattern);
        return cacheRequest;
    }

    private static List<SnapshotNode> BuildNodes(
        AutomationElement element,
        int depth,
        int maxDepth,
        ITreeWalker walker,
        bool forceInclude = false)
    {
        if (depth > maxDepth)
        {
            return [];
        }

        var children = new List<SnapshotNode>();
        foreach (var child in EnumerateChildren(element, walker))
        {
            children.AddRange(BuildNodes(child, depth + 1, maxDepth, walker));
        }

        var disposition = DecideDisposition(element, children.Count, forceInclude);
        return disposition switch
        {
            SnapshotNodeDisposition.Include => [new SnapshotNode(element, children)],
            SnapshotNodeDisposition.Flatten => children,
            _ => []
        };
    }

    private static SnapshotNodeDisposition DecideDisposition(AutomationElement element, int childCount, bool forceInclude)
    {
        if (forceInclude)
        {
            return SnapshotNodeDisposition.Include;
        }

        if (!element.IsVisible)
        {
            return childCount > 0 ? SnapshotNodeDisposition.Flatten : SnapshotNodeDisposition.Skip;
        }

        if (IsAlwaysIncludedControl(element.SnapshotControlType))
        {
            return SnapshotNodeDisposition.Include;
        }

        if (IsSemanticContainer(element.SnapshotControlType))
        {
            return childCount > 0 ? SnapshotNodeDisposition.Include : SnapshotNodeDisposition.Skip;
        }

        if (IsGenericContainer(element.SnapshotControlType))
        {
            if (childCount == 0)
            {
                return element.HasPresentation ? SnapshotNodeDisposition.Include : SnapshotNodeDisposition.Skip;
            }

            return element.HasPresentation ? SnapshotNodeDisposition.Include : SnapshotNodeDisposition.Flatten;
        }

        if (IsInformationalControl(element.SnapshotControlType))
        {
            return element.HasPresentation ? SnapshotNodeDisposition.Include : SnapshotNodeDisposition.Skip;
        }

        if (element.SnapshotControlType == ControlType.Custom)
        {
            if (element.IsInvocable || element.IsKeyboardFocusable)
            {
                return SnapshotNodeDisposition.Include;
            }

            if (childCount == 0)
            {
                return element.HasPresentation ? SnapshotNodeDisposition.Include : SnapshotNodeDisposition.Skip;
            }

            return element.HasPresentation ? SnapshotNodeDisposition.Include : SnapshotNodeDisposition.Flatten;
        }

        return childCount > 0 ? SnapshotNodeDisposition.Flatten : SnapshotNodeDisposition.Skip;
    }

    private static bool IsAlwaysIncludedControl(ControlType controlType)
    {
        return controlType is ControlType.Button
            or ControlType.CheckBox
            or ControlType.ComboBox
            or ControlType.DataItem
            or ControlType.Edit
            or ControlType.Hyperlink
            or ControlType.ListItem
            or ControlType.MenuItem
            or ControlType.ProgressBar
            or ControlType.RadioButton
            or ControlType.Slider
            or ControlType.Spinner
            or ControlType.SplitButton
            or ControlType.TabItem
            or ControlType.TreeItem;
    }

    private static bool IsSemanticContainer(ControlType controlType)
    {
        return controlType is ControlType.DataGrid
            or ControlType.List
            or ControlType.Menu
            or ControlType.MenuBar
            or ControlType.StatusBar
            or ControlType.Tab
            or ControlType.Table
            or ControlType.ToolBar
            or ControlType.Tree
            or ControlType.Window;
    }

    private static bool IsGenericContainer(ControlType controlType)
    {
        return controlType is ControlType.Document
            or ControlType.Group
            or ControlType.Pane;
    }

    private static bool IsInformationalControl(ControlType controlType)
    {
        return controlType is ControlType.Header
            or ControlType.HeaderItem
            or ControlType.Image
            or ControlType.Text
            or ControlType.ToolTip;
    }

    private static IEnumerable<AutomationElement> EnumerateChildren(AutomationElement element, ITreeWalker walker)
    {
        AutomationElement? child;
        try
        {
            child = walker.GetFirstChild(element);
        }
        catch (Exception)
        {
            yield break;
        }

        while (child is not null)
        {
            yield return child;

            try
            {
                child = walker.GetNextSibling(child);
            }
            catch (Exception)
            {
                yield break;
            }
        }
    }

    private enum SnapshotNodeDisposition
    {
        Skip,
        Flatten,
        Include,
    }
}
