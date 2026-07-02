namespace Spectre.Console;

internal sealed class StringBuffer : IDisposable
{
    private readonly string _text;
    private readonly int _length;

    public int Position { get; private set; }
    public bool Eof => Position >= _length;

    public StringBuffer(string text)
    {
        _text = text ?? string.Empty;
        _length = _text.Length;
        Position = 0;
    }

    public void Dispose()
    {
    }

    public char Expect(char character)
    {
        var read = Read();
        if (read != character)
        {
            throw new InvalidOperationException($"Expected '{character}', but found '{read}'");
        }

        return read;
    }

    public char Peek()
    {
        return Eof ? '\0' : _text[Position];
    }

    public char Read()
    {
        return Eof ? '\0' : _text[Position++];
    }

    // Zero-copy view of a scanned range of the source, for slicing token text without a StringBuilder.
    public ReadOnlySpan<char> AsSpan(int start, int length)
    {
        return _text.AsSpan(start, length);
    }

    public string Substring(int start, int length)
    {
        return _text.Substring(start, length);
    }
}
