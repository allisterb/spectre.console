namespace Spectre.Console;

internal sealed class MarkupTokenizer : IDisposable
{
    private readonly StringBuffer _reader;

    public MarkupToken? Current { get; private set; }

    public MarkupTokenizer(string text)
    {
        _reader = new StringBuffer(text ?? throw new ArgumentNullException(nameof(text)));
    }

    public void Dispose()
    {
        _reader.Dispose();
    }

    public bool MoveNext()
    {
        if (_reader.Eof)
        {
            return false;
        }

        var current = _reader.Peek();
        return current == '[' ? ReadMarkup() : ReadText();
    }

    private bool ReadText()
    {
        var position = _reader.Position;
        var builder = new StringBuilder();

        var encounteredClosing = false;
        while (!_reader.Eof)
        {
            var current = _reader.Peek();
            if (current == '[')
            {
                // markup encountered. Stop processing.
                break;
            }

            // If we find a closing tag (']') there must be two of them.
            if (current == ']')
            {
                if (encounteredClosing)
                {
                    _reader.Read();
                    encounteredClosing = false;
                    continue;
                }

                encounteredClosing = true;
            }
            else
            {
                if (encounteredClosing)
                {
                    throw new InvalidOperationException(
                        $"Encountered unescaped ']' token at position {_reader.Position}");
                }
            }

            builder.Append(_reader.Read());
        }

        if (encounteredClosing)
        {
            throw new InvalidOperationException($"Encountered unescaped ']' token at position {_reader.Position}");
        }

        Current = new MarkupToken(MarkupTokenKind.Text, builder.ToString(), position);
        return true;
    }

    private bool ReadMarkup()
    {
        var position = _reader.Position;

        _reader.Read();

        if (_reader.Eof)
        {
            throw new InvalidOperationException($"Encountered malformed markup tag at position {_reader.Position}.");
        }

        var current = _reader.Peek();
        switch (current)
        {
            case '[':
                // No markup but instead escaped markup in text.
                _reader.Read();
                Current = new MarkupToken(MarkupTokenKind.Text, "[", position);
                return true;
            case '/':
                // Markup closed.
                _reader.Read();

                if (_reader.Eof)
                {
                    throw new InvalidOperationException(
                        $"Encountered malformed markup tag at position {_reader.Position}.");
                }

                current = _reader.Peek();
                if (current != ']')
                {
                    throw new InvalidOperationException(
                        $"Encountered malformed markup tag at position {_reader.Position}.");
                }

                _reader.Read();
                Current = new MarkupToken(MarkupTokenKind.Close, string.Empty, position);
                return true;
        }

        // Read the "content" of the markup until we find the end-of-markup
        var builder = new StringBuilder();
        var encounteredOpening = false;
        var encounteredClosing = false;
        var wordStart = 0;   // index in `builder` where the current space-delimited style part begins
        while (!_reader.Eof)
        {
            // Whether the current style part is a "link=" (whose value may contain [ and ] that must be escaped).
            // Checked incrementally against `builder` rather than rebuilding builder.ToString().Split(' ').Last()
            // every character, which was O(n^2) in time and allocations for each markup tag.
            var currentStylePartCanContainMarkup = StartsWithLink(builder, wordStart);
            current = _reader.Peek();

            if (currentStylePartCanContainMarkup)
            {
                switch (current)
                {
                    case ']' when !encounteredOpening:
                        if (encounteredClosing)
                        {
                            builder.Append(_reader.Read());
                            encounteredClosing = false;
                            continue;
                        }

                        _reader.Read();
                        encounteredClosing = true;
                        continue;

                    case '[' when !encounteredClosing:
                        if (encounteredOpening)
                        {
                            builder.Append(_reader.Read());
                            encounteredOpening = false;
                            continue;
                        }

                        _reader.Read();
                        encounteredOpening = true;
                        continue;
                }
            }
            else
            {
                switch (current)
                {
                    case ']':
                        _reader.Read();
                        encounteredClosing = true;
                        break;
                    case '[':
                        _reader.Read();
                        encounteredOpening = true;
                        break;
                }
            }

            if (encounteredClosing)
            {
                break;
            }

            if (encounteredOpening)
            {
                throw new InvalidOperationException(
                    $"Encountered malformed markup tag at position {_reader.Position - 1}.");
            }

            var ch = _reader.Read();
            builder.Append(ch);
            if (ch == ' ')
            {
                // Next space-delimited style part starts after this space.
                wordStart = builder.Length;
            }
        }

        if (_reader.Eof)
        {
            throw new InvalidOperationException($"Encountered malformed markup tag at position {_reader.Position}.");
        }

        Current = new MarkupToken(MarkupTokenKind.Open, builder.ToString(), position);
        return true;
    }

    // Case-insensitive test for the "link=" prefix at <paramref name="wordStart"/> in <paramref name="builder"/>,
    // matching the original builder.ToString().Split(' ').Last().StartsWith("link=", OrdinalIgnoreCase) without
    // allocating. The markup keyword is ASCII, so ordinal lowercasing of A-Z suffices.
    private static bool StartsWithLink(StringBuilder builder, int wordStart)
    {
        const string prefix = "link=";
        if (builder.Length - wordStart < prefix.Length)
        {
            return false;
        }

        for (var i = 0; i < prefix.Length; i++)
        {
            var c = builder[wordStart + i];
            if (c is >= 'A' and <= 'Z')
            {
                c = (char)(c + 32);
            }

            if (c != prefix[i])
            {
                return false;
            }
        }

        return true;
    }
}