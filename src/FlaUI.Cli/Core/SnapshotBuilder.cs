using System.Buffers;
using System.Globalization;
using FlaUI.Cli.Rpc;
using FlaUI.Core.AutomationElements;

namespace FlaUI.Cli.Core;

internal static class SnapshotBuilder
{
    private const int IndentSize = 2;
    private const int IndentCacheDepth = 32;
    private const int MaxFormattedElementRefLength = 11;
    private const int MaxStackAllocatedIndentLength = 256;
    private static readonly string[] IndentCache = [
        .. Enumerable.Range(0, IndentCacheDepth + 1)
            .Select(depth => new string(' ', depth * IndentSize))];

    public static Dictionary<ElementRef, AutomationElement> Build(Window window, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(writer);

        var context = new WriteContext();
        var rootNode = SnapshotNodeBuilder.Build(window);
        WriteNode(writer, rootNode, 0, context);
        return context.Elements;
    }

    private static void WriteNode(
        TextWriter writer,
        SnapshotNode node,
        int depth,
        WriteContext context)
    {
        var elementRef = context.Register(node.Element);
        WriteIndent(writer, depth);
        writer.Write("- ");
        writer.Write(node.Role);

        if (!string.IsNullOrWhiteSpace(node.Name))
        {
            writer.Write(' ');
            WriteQuotedValue(writer, node.Name);
        }

        writer.Write(" [ref=");
        WriteElementRef(writer, elementRef);
        writer.Write(']');
        WriteStateFlags(writer, node.States);

        if (node.Children.Count > 0)
        {
            if (!string.IsNullOrWhiteSpace(node.Value))
            {
                writer.Write(" [value=");
                WriteQuotedValue(writer, node.Value);
                writer.Write(']');
            }

            writer.WriteLine(":");
            foreach (var child in node.Children)
            {
                WriteNode(writer, child, depth + 1, context);
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(node.Value))
        {
            writer.Write(": ");
            writer.WriteLine(node.Value);
            return;
        }

        writer.WriteLine();
    }

    private static void WriteStateFlags(TextWriter writer, IReadOnlyList<string> states)
    {
        foreach (var state in states)
        {
            writer.Write(" [");
            writer.Write(state);
            writer.Write(']');
        }
    }

    private static void WriteQuotedValue(TextWriter writer, string value)
    {
        writer.Write('"');

        var remaining = value.AsSpan();
        while (!remaining.IsEmpty)
        {
            var escapeIndex = remaining.IndexOfAny('\\', '"');
            if (escapeIndex < 0)
            {
                writer.Write(remaining);
                break;
            }

            if (escapeIndex > 0)
            {
                writer.Write(remaining[..escapeIndex]);
            }

            writer.Write('\\');
            writer.Write(remaining[escapeIndex]);
            remaining = remaining[(escapeIndex + 1)..];
        }

        writer.Write('"');
    }

    private static void WriteElementRef(TextWriter writer, ElementRef elementRef)
    {
        Span<char> buffer = stackalloc char[MaxFormattedElementRefLength];
        buffer[0] = 'e';
        if (!elementRef.Element.TryFormat(buffer[1..], out var written, provider: CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("Failed to format element ref.");
        }

        writer.Write(buffer[..(written + 1)]);
    }

    private static void WriteIndent(TextWriter writer, int depth)
    {
        if (depth <= IndentCacheDepth)
        {
            writer.Write(IndentCache[depth]);
            return;
        }

        var indentLength = depth * IndentSize;
        if (indentLength <= MaxStackAllocatedIndentLength)
        {
            Span<char> indent = stackalloc char[indentLength];
            indent.Fill(' ');
            writer.Write(indent);
            return;
        }

        var rented = ArrayPool<char>.Shared.Rent(indentLength);
        try
        {
            var indent = rented.AsSpan(0, indentLength);
            indent.Fill(' ');
            writer.Write(indent);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(rented);
        }
    }

    private sealed class WriteContext
    {
        private int _nextElementId;

        public Dictionary<ElementRef, AutomationElement> Elements { get; } = [];

        public ElementRef Register(AutomationElement element)
        {
            var elementRef = new ElementRef(++_nextElementId);
            Elements[elementRef] = element;
            return elementRef;
        }
    }
}
