namespace Spectre.Console;

/// <summary>
/// Contains extension methods for <see cref="string"/>.
/// </summary>
public static class StringExtensions
{
    // Cache whether or not internally normalized line endings
    // already are normalized. No reason to do yet another replace if it is.
    private static readonly bool _alreadyNormalized
        = Environment.NewLine.Equals("\n", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Escapes text so that it won’t be interpreted as markup.
    /// </summary>
    /// <param name="text">The text to escape.</param>
    /// <returns>A string that is safe to use in markup.</returns>
    public static string EscapeMarkup(this string? text)
    {
        if (text == null)
        {
            return string.Empty;
        }

        return text
            .ReplaceExact("[", "[[")
            .ReplaceExact("]", "]]");
    }

    /// <summary>
    /// Removes markup from the specified string.
    /// </summary>
    /// <param name="text">The text to remove markup from.</param>
    /// <returns>A string that does not have any markup.</returns>
    public static string RemoveMarkup(this string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var result = new StringBuilder();

        var tokenizer = new MarkupTokenizer(text);
        while (tokenizer.MoveNext())
        {
            if (tokenizer.Current is { Kind: MarkupTokenKind.Text } token)
            {
                result.Append(token.Value);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Gets the cell width of the specified text.
    /// </summary>
    /// <param name="text">The text to get the cell width of.</param>
    /// <returns>The cell width of the text.</returns>
    public static int GetCellWidth(this string text)
    {
        return Cell.GetCellLength(text);
    }

    internal static string CapitalizeFirstLetter(this string? text, CultureInfo? culture = null)
    {
        if (text == null)
        {
            return string.Empty;
        }

        culture ??= CultureInfo.InvariantCulture;

        if (text.Length > 0 && char.IsLower(text[0]))
        {
            text = string.Format(culture, "{0}{1}", char.ToUpper(text[0], culture), text.Substring(1));
        }

        return text;
    }

    internal static string? RemoveNewLines(this string? text)
    {
        return text?.ReplaceExact("\r\n", string.Empty)
            ?.ReplaceExact("\n", string.Empty);
    }

    internal static string NormalizeNewLines(this string? text, bool native = false)
    {
        text = text?.ReplaceExact("\r\n", "\n");
        text ??= string.Empty;

        if (native && !_alreadyNormalized)
        {
            text = text.ReplaceExact("\n", Environment.NewLine);
        }

        return text;
    }

    internal static string[] SplitLines(this string text)
    {
        var result = text?.NormalizeNewLines()?.Split(['\n'], StringSplitOptions.None);
        return result ?? [];
    }

    // Splits into alternating runs of whitespace / non-whitespace as zero-copy slices of the source memory.
    // Whitespace runs are dropped when RemoveEmptyEntries is set. This is the allocation-free core used by
    // Paragraph.Append (a Segment per slice) on the markup write path.
    internal static List<ReadOnlyMemory<char>> SplitWords(this ReadOnlyMemory<char> text, StringSplitOptions options = StringSplitOptions.None)
    {
        var result = new List<ReadOnlyMemory<char>>();
        var span = text.Span;

        var i = 0;
        while (i < span.Length)
        {
            var start = i;
            var isWhiteSpace = char.IsWhiteSpace(span[i]);
            while (i < span.Length && char.IsWhiteSpace(span[i]) == isWhiteSpace)
            {
                i++;
            }

            if (isWhiteSpace && options == StringSplitOptions.RemoveEmptyEntries)
            {
                continue;
            }

            result.Add(text[start..i]);
        }

        return result;
    }

    internal static string[] SplitWords(this string word, StringSplitOptions options = StringSplitOptions.None)
    {
        var slices = word.AsMemory().SplitWords(options);
        var result = new string[slices.Count];
        for (var i = 0; i < slices.Count; i++)
        {
            result[i] = slices[i].ToString();
        }

        return result;
    }

    internal static string Repeat(this string text, int count)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (count <= 0)
        {
            return string.Empty;
        }

        if (count == 1)
        {
            return text;
        }

        return string.Concat(Enumerable.Repeat(text, count));
    }

    internal static string ReplaceExact(this string text, string oldValue, string? newValue)
    {
#if NETSTANDARD2_0
        return text.Replace(oldValue, newValue);
#else
        return text.Replace(oldValue, newValue, StringComparison.Ordinal);
#endif
    }

    internal static bool ContainsExact(this string text, string value)
    {
#if NETSTANDARD2_0
        return text.Contains(value);
#else
        return text.Contains(value, StringComparison.Ordinal);
#endif
    }

#if NETSTANDARD2_0
    internal static bool Contains(this string target, string value, System.StringComparison comparisonType)
    {
        return target.IndexOf(value, comparisonType) != -1;
    }
#endif

    /// <summary>
    /// "Masks" every character in a string.
    /// </summary>
    /// <param name="value">String value to mask.</param>
    /// <param name="mask">Character to use for masking.</param>
    /// <returns>Masked string.</returns>
    public static string Mask(this string value, char? mask)
    {
        if (mask is null)
        {
            return string.Empty;
        }

        return new string(mask.Value, value.Length);
    }

    /// <summary>
    /// Highlights the first text match in provided value.
    /// </summary>
    /// <param name="value">Input value.</param>
    /// <param name="searchText">Text to search for.</param>
    /// <param name="highlightStyle">The style to apply to the matched text.</param>
    /// <returns>Markup of input with the first matched text highlighted.</returns>
    internal static string Highlight(this string value, string searchText, Style? highlightStyle)
    {
        ArgumentNullException.ThrowIfNull(value);

        ArgumentNullException.ThrowIfNull(searchText);

        ArgumentNullException.ThrowIfNull(highlightStyle);

        if (searchText.Length == 0)
        {
            return value;
        }

        var foundSearchPattern = false;
        var builder = new StringBuilder();
        using var tokenizer = new MarkupTokenizer(value);
        while (tokenizer.MoveNext())
        {
            if (tokenizer.Current is not { } token)
            {
                continue;
            }

            switch (token.Kind)
            {
                case MarkupTokenKind.Text:
                    {
                        var tokenValue = token.Value;
                        if (tokenValue.Length == 0)
                        {
                            break;
                        }

                        if (foundSearchPattern)
                        {
                            builder.Append(tokenValue);
                            break;
                        }

                        var index = tokenValue.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);
                        if (index == -1)
                        {
                            builder.Append(tokenValue);
                            break;
                        }

                        foundSearchPattern = true;
                        var before = tokenValue.Substring(0, index);
                        var match = tokenValue.Substring(index, searchText.Length);
                        var after = tokenValue.Substring(index + searchText.Length);

                        builder
                            .Append(before)
                            .AppendWithStyle(highlightStyle, match)
                            .Append(after);

                        break;
                    }

                case MarkupTokenKind.Open:
                    {
                        builder.Append("[" + token.Value + "]");
                        break;
                    }

                case MarkupTokenKind.Close:
                    {
                        builder.Append("[/]");
                        break;
                    }

                default:
                    {
                        throw new InvalidOperationException("Unknown markup token kind.");
                    }
            }
        }

        return builder.ToString();
    }
}