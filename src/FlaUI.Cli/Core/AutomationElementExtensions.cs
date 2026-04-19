using System.Globalization;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace FlaUI.Cli.Core;

internal static class AutomationElementExtensions
{
    extension(AutomationElement element)
    {
        public ControlType SnapshotControlType => ReadControlType(element);

        public string? SnapshotName => ReadDisplayName(element);

        public string? SnapshotValue => ReadSnapshotValue(element);

        public IReadOnlyList<string> SnapshotStates => ReadStates(element);

        public bool IsVisible => (!element.Properties.BoundingRectangle.TryGetValue(out var bounds)
            || (bounds.Width > 0 && bounds.Height > 0))
            && !(element.Properties.IsOffscreen.TryGetValue(out var offscreen) && offscreen);

        public bool IsKeyboardFocusable =>
            element.Properties.IsKeyboardFocusable.TryGetValue(out var keyboardFocusable) && keyboardFocusable;

        public bool IsInvocable => element.Patterns.Invoke.TryGetPattern(out _);

        public bool HasPresentation => ReadDisplayName(element) is not null
            || ReadSnapshotValue(element) is not null
            || ReadStates(element).Count > 0;
    }

    private static ControlType ReadControlType(AutomationElement element)
    {
        return element.Properties.ControlType.TryGetValue(out var controlType)
            ? controlType
            : ControlType.Custom;
    }

    private static string? ReadDisplayName(AutomationElement element)
    {
        if (element.Properties.Name.TryGetValue(out var name))
        {
            return name;
        }

        if (element.Properties.AutomationId.TryGetValue(out var automationId))
        {
            return automationId;
        }

        return null;
    }

    private static string? ReadValue(AutomationElement element, ControlType controlType)
    {
        return controlType switch
        {
            ControlType.Edit or ControlType.ComboBox => ReadValuePatternText(element),
            ControlType.Spinner => ReadValuePatternText(element) ?? ReadRangeValueText(element),
            ControlType.Slider or ControlType.ProgressBar => ReadRangeValueText(element),
            _ => null,
        };
    }

    private static string? ReadValuePatternText(AutomationElement element)
    {
        return element.Patterns.Value.TryGetPattern(out var valuePattern)
            && valuePattern.Value.TryGetValue(out var value)
                ? value
                : null;
    }

    private static string? ReadSnapshotValue(AutomationElement element)
    {
        var name = ReadDisplayName(element);
        var value = ReadValue(element, ReadControlType(element));
        return string.Equals(name, value, StringComparison.Ordinal) ? null : value;
    }

    private static string? ReadRangeValueText(AutomationElement element)
    {
        if (!element.Patterns.RangeValue.TryGetPattern(out var rangeValuePattern)
            || !rangeValuePattern.Value.TryGetValue(out var value))
        {
            return null;
        }

        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static List<string> ReadStates(AutomationElement element)
    {
        List<string> states = [];

        if (element.Properties.IsEnabled.TryGetValue(out var isEnabled) && !isEnabled)
        {
            states.Add("disabled");
        }

        if (element.Patterns.Value.TryGetPattern(out var valuePattern)
            && valuePattern.IsReadOnly.TryGetValue(out var valueReadOnly)
            && valueReadOnly)
        {
            states.Add("readonly");
        }
        else if (element.Patterns.RangeValue.TryGetPattern(out var rangeValuePattern)
            && rangeValuePattern.IsReadOnly.TryGetValue(out var rangeReadOnly)
            && rangeReadOnly)
        {
            states.Add("readonly");
        }

        if (element.Patterns.Toggle.TryGetPattern(out var togglePattern)
            && togglePattern.ToggleState.TryGetValue(out var toggleState))
        {
            if (toggleState == ToggleState.On)
            {
                states.Add("checked");
            }
            else if (toggleState == ToggleState.Indeterminate)
            {
                states.Add("mixed");
            }
        }

        if (element.Patterns.ExpandCollapse.TryGetPattern(out var expandCollapsePattern)
            && expandCollapsePattern.ExpandCollapseState.TryGetValue(out var expandCollapseState))
        {
            if (expandCollapseState == ExpandCollapseState.Expanded)
            {
                states.Add("expanded");
            }
            else if (expandCollapseState == ExpandCollapseState.Collapsed)
            {
                states.Add("collapsed");
            }
            else if (expandCollapseState == ExpandCollapseState.PartiallyExpanded)
            {
                states.Add("partial");
            }
        }

        if (element.Patterns.SelectionItem.TryGetPattern(out var selectionItemPattern)
            && selectionItemPattern.IsSelected.TryGetValue(out var isSelected)
            && isSelected)
        {
            states.Add("selected");
        }

        return states;
    }

}
