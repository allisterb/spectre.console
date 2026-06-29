namespace Spectre.Console;

internal sealed class LiveDisplayRenderer : IRenderHook
{
    private readonly IAnsiConsole _console;
    private readonly LiveDisplayContext _context;
    public LiveDisplayRenderer(IAnsiConsole console, LiveDisplayContext context)
    {
        _console = console;
        _context = context;
    }

    public void Started()
    {
        _console.Cursor.Hide();
    }

    public void Completed(bool autoclear)
    {
        lock (_context.Lock)
        {
            if (autoclear)
            {
                // _console.Write(_context.Live.RestoreCursor());
                _console.Clear(true);
            }
            else
            {
                if (_context.Live.HasRenderable && _context.Live.DidOverflow)
                {
                    // Redraw the whole live renderable
                    // _console.Write(_context.Live.RestoreCursor());
                    _console.Clear(true);
                    _context.Live.Overflow = VerticalOverflow.Visible;
                    _console.Write(_context.Live.Target);
                }

                _console.WriteLine();
            }

            // _console.Cursor.Show();
        }
    }

    public IEnumerable<IRenderable> Process(RenderOptions options, IEnumerable<IRenderable> renderables)
    {
        lock (_context.Lock)
        {
            // Re-entered by the internal cursor/clear writes PositionCursor performs on a real ANSI backend:
            // pass them through untouched so we neither recurse nor re-render the live content.
            if (_context.Live.SuppressRenderHook)
            {
                foreach (var renderable in renderables)
                {
                    yield return renderable;
                }

                yield break;
            }

            yield return _context.Live.PositionCursor(options);

            foreach (var renderable in renderables)
            {
                yield return renderable;
            }

            yield return _context.Live;
        }
    }
}