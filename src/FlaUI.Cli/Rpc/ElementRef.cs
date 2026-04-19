using System.Globalization;
using MessagePack;

namespace FlaUI.Cli.Rpc;

[MessagePackObject]
internal sealed record ElementRef
{
    public ElementRef(int element)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(element, nameof(element));
        Element = element;
    }

    [Key(0)]
    public int Element { get; }

    public override string ToString() => $"e{Element}";

    public static ElementRef Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        if (value[0] == 'e')
        {
            var parsedElementId = int.Parse(value.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture);
            return new ElementRef(parsedElementId);
        }

        throw new FormatException($"Invalid element ref format: {value}");
    }
}
