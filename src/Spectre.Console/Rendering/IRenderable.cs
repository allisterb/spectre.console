namespace Spectre.Console.Rendering;

/// <summary>
/// Represents something that can be rendered to the console.
/// </summary>
public interface IRenderable
{
    /// <summary>
    /// Measures the renderable object.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <param name="maxWidth">The maximum allowed width.</param>
    /// <returns>The minimum and maximum width of the object.</returns>
    Measurement Measure(RenderOptions options, int maxWidth);

    /// <summary>
    /// Renders the object.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <param name="maxWidth">The maximum allowed width.</param>
    /// <returns>A collection of segments.</returns>
    IEnumerable<Segment> Render(RenderOptions options, int maxWidth);
}

/// <summary>
/// Contains extension methods for <see cref="IRenderable"/>.
/// </summary>
public static class RenderableExtensions
{
    /// <summary>
    /// Gets the segments for a renderable using the specified console.
    /// </summary>
    /// <param name="renderable">The renderable.</param>
    /// <param name="console">The console.</param>
    /// <returns>An enumerable containing segments representing the specified <see cref="IRenderable"/>.</returns>
    public static IEnumerable<Segment> GetSegments(this IRenderable renderable, IAnsiConsole console)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(renderable);

        var context = RenderOptions.Create(console, console.Profile.Capabilities);
        var renderables = console.Pipeline.Process(context, [renderable]);

        return GetSegments(console, context, renderables);
    }

    /// <summary>
    /// Gets the segments for a renderable using the specified console and a pre-built <see cref="RenderOptions"/>.
    /// </summary>
    /// <param name="renderable">The renderable.</param>
    /// <param name="console">The console.</param>
    /// <param name="options">The render options to use. <see cref="RenderOptions"/> is immutable, so a caller that
    /// renders repeatedly at a fixed size may cache and reuse one instance to avoid re-allocating it per render.</param>
    /// <returns>An enumerable containing segments representing the specified <see cref="IRenderable"/>.</returns>
    public static IEnumerable<Segment> GetSegments(this IRenderable renderable, IAnsiConsole console, RenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(renderable);
        ArgumentNullException.ThrowIfNull(options);

        var renderables = console.Pipeline.Process(options, [renderable]);

        return GetSegments(console, options, renderables);
    }

    private static IEnumerable<Segment> GetSegments(IAnsiConsole console, RenderOptions options, IEnumerable<IRenderable> renderables)
    {
        var result = new List<Segment>();
        foreach (var renderable in renderables)
        {
            result.AddRange(renderable.Render(options, console.Profile.Width));
        }

        return Segment.Merge(result);
    }
}