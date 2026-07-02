namespace Spectre.Console;

internal readonly struct MarkupToken
{
    public MarkupTokenKind Kind { get; }
    public string Value { get; }
    public int Position { get; }

    public MarkupToken(MarkupTokenKind kind, string value, int position)
    {
        Kind = kind;
        Value = value ?? throw new ArgumentNullException(nameof(value));
        Position = position;
    }
}