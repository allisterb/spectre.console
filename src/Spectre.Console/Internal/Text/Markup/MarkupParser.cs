namespace Spectre.Console;

internal static class MarkupParser
{
    public static Paragraph Parse(string text, Style? style = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        style ??= Style.Plain;

        var result = new Paragraph();
        using var tokenizer = new MarkupTokenizer(text);

        // Track the effective (combined) style incrementally: `effective` is the base style combined with every
        // currently-open tag, and `restore` holds the style to roll back to on each close. This replaces
        // style.Combine(stack.Reverse()) on every text token (a LINQ allocation plus a full re-fold each time).
        var restore = new Stack<Style>();
        var effective = style;

        while (tokenizer.MoveNext())
        {
            var token = tokenizer.Current;
            if (token == null)
            {
                break;
            }

            if (token.Kind == MarkupTokenKind.Open)
            {
                var parsedStyle = string.IsNullOrEmpty(token.Value) ? Style.Plain : StyleParser.Parse(token.Value);
                restore.Push(effective);
                effective = effective.Combine(parsedStyle);
            }
            else if (token.Kind == MarkupTokenKind.Close)
            {
                if (restore.Count == 0)
                {
                    throw new InvalidOperationException($"Encountered closing tag when none was expected near position {token.Position}.");
                }

                effective = restore.Pop();
            }
            else if (token.Kind == MarkupTokenKind.Text)
            {
                result.Append(Emoji.Replace(token.Value), effective);
            }
            else
            {
                throw new InvalidOperationException("Encountered unknown markup token.");
            }
        }

        if (restore.Count > 0)
        {
            throw new InvalidOperationException("Unbalanced markup stack. Did you forget to close a tag?");
        }

        return result;
    }
}