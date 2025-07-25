// #define RASTERENUMERATOR_USE_TERNARY
using System.Collections;

using SixLabors.ImageSharp;

namespace Donjon;

/// <summary>
/// an enumerator that produces indices to traverse a raster in row-major or column-major order
/// </summary>
/// <param name="rowEnd"></param>
/// <param name="colEnd"></param>
/// <param name="rowStart"></param>
/// <param name="colStart"></param>
/// <param name="rowMajor">whether to increment across columns before incrementing the row</param>
/// <param name="inclusive">are <paramref name="rowEnd"/> and <paramref name="colEnd"/> accessible indices</param>
public class RasterEnumerator(int rowEnd,
                              int colEnd,
                              int rowStart = 0,
                              int colStart = 0,
                              int step = 1,
                              bool rowMajor = true,
                              bool inclusive = false)
    : IEnumerator<(int r, int c)>, IEnumerable<(int r, int c)>
{
    private bool InBounds(int a, int b) => inclusive ? a <= b : a < b;

    private (int r, int c) _start = rowMajor ? (Math.Min(rowStart, rowEnd), Math.Min(colStart, colEnd) - step)
                                            : (Math.Min(rowStart, rowEnd) - step, Math.Min(colStart, colEnd));
    private (int r, int c) _current = rowMajor ? (Math.Min(rowStart, rowEnd), Math.Min(colStart, colEnd) - step)
                                            : (Math.Min(rowStart, rowEnd) - step, Math.Min(colStart, colEnd));

    public (int r, int c) Current => _current;

    public (int r, int c) Min { get; } = (Math.Min(rowStart, rowEnd), Math.Min(colStart, colEnd));
    public (int r, int c) Max { get; } = (Math.Max(rowStart, rowEnd), Math.Max(colStart, colEnd));

    object IEnumerator.Current => Current;

    public void Dispose() { }
    public void Reset() => _current = _start;

    protected bool ColumnMajorMoveNext()
    {
#if RASTERENUMERATOR_USE_TERNARY
        (_current, var ret) = InBounds(_current.r + step, Max.r)
            ? ((r: _current.r + step, c: _current.c), true) 
            : !InBounds(_current.r + step, Max.r) && InBounds(_current.c + step, Max.c)
                ? ((Min.r, c: _current.c + step), true)
                : (_current, false);
        return ret;
#else
        if (InBounds(Current.r + step, Max.r))
        {
            _current.r += step;
            return true;
        }
        else if (InBounds(Current.c + step, Max.c))
        {
            _current = (r: Min.r, c: _current.c + step);
            return true;
        }
        else
        {
            return false;
        }
#endif
    }

    protected bool RowMajorMoveNext()
    {
#if RASTERENUMERATOR_USE_TERNARY
        (_current, var ret) = InBounds(_current.c + step, Max.c)
            ? ((r: _current.r, c: _current.c + step), true)
            : InBounds(_current.r + step, Max.r)
                ? ((_current.r + step, c: Min.c), true)
                : (_current, false);
        return ret;
#else
        // if raster can get away with the minor only...
        if (InBounds(_current.c + step, Max.c))
        {
            _current.c += step;
            return true;
        }
        // otherwise, if raster can EOL
        else if (InBounds(_current.r + step, Max.r))
        {
            _current = (r: _current.r + step, c: Min.c);
            return true;
        }
        // or, just stop
        else { return false; }
#endif
    }

    public bool MoveNext() => rowMajor ? RowMajorMoveNext() : ColumnMajorMoveNext();

    public IEnumerator<(int r, int c)> GetEnumerator() => this;

    IEnumerator IEnumerable.GetEnumerator() => this;
}

/// <summary>
/// Semantics of a rectangle on the grid
/// </summary>
/// <example>
/// FIXME: Dim2d and RasterEnumerator need more scrutiny (not every rect to rasterize will need room semantics)
/// 1 - sizewise : a rectangle width=1 represents 1 cell width << this is more sensible, which means sound_room (via emplace_room_collisiontest) needs to be careful
/// 2 - indexwise: a rectangle width=0 represents 1 cell width
/// In .NET, Rectangle.Height == 0 means the rectangle covers no rows (empty). (sizewise)
///     Rectangle.Bottom = Top + Height (exclusive).
///     Rectangle.Right = Left + Width (exclusive).
/// Adhere to the .NET sizewise definition.
/// </example>
public static class Dim2d
{
    const int DEFAULT_STEP = 1;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="startX"></param>
    /// <param name="endX"></param>
    /// <param name="startY"></param>
    /// <param name="endY"></param>
    /// <param name="rstep"></param>
    /// <param name="cstep"></param>
    /// <returns></returns>
    public static IEnumerable<(int r, int c)> RangeUpperExclusive(
        int startX, int endX, int startY, int endY,
        int rstep = DEFAULT_STEP, int cstep = DEFAULT_STEP)
        => RangeInclusive(startX, endX - 1, startY, endY - 1, rstep, cstep);

    /// <summary>
    /// require a h/w > 1, This is the correct form to use for room rectangles
    /// </summary>
    /// <param name="r"></param>
    /// <param name="rstep"></param>
    /// <param name="cstep"></param>
    /// <returns></returns>
    public static IEnumerable<(int r, int c)> RangeUpperExclusive(
        Rectangle r, int rstep = DEFAULT_STEP, int cstep = DEFAULT_STEP)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(r.Height, 1, nameof(r.Height)); // In .NET, Rectangle.Height == 0 means the rectangle covers no rows (empty).
        ArgumentOutOfRangeException.ThrowIfLessThan(r.Width, 1, nameof(r.Width)); // In .NET, Rectangle.Width == 0 means the rectangle covers no cols (empty).
        // Rectangle.Bottom = Top + Height (exclusive).
        // Rectangle.Right = Left + Width (exclusive).
        return RangeUpperExclusive(r.Top, r.Bottom, r.Left, r.Right, rstep, cstep);
    }

    /// <summary>
    /// Get Row-col tuples for each cell in the rectangle
    /// </summary>
    public static IEnumerable<(int r, int c)> RangeInclusive(Coord a, Coord b, int rstep = DEFAULT_STEP, int cstep = DEFAULT_STEP)
    => RangeInclusive(a.r, b.r, a.c, b.c, rstep, cstep);

    /// <summary>
    /// Get Row-col tuples for each cell in the rectangle
    /// </summary>
    public static IEnumerable<(int r, int c)> RangeInclusive(Rectangle r, int rstep = DEFAULT_STEP, int cstep = DEFAULT_STEP)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(r.Height, 1, nameof(r.Height)); // In .NET, Rectangle.Height == 0 means the rectangle covers no rows (empty).
        ArgumentOutOfRangeException.ThrowIfLessThan(r.Width, 1, nameof(r.Width)); // In .NET, Rectangle.Width == 0 means the rectangle covers no cols (empty).
        ArgumentOutOfRangeException.ThrowIfLessThan(r.Bottom, r.Top);
        ArgumentOutOfRangeException.ThrowIfLessThan(r.Right, r.Left);
        return RangeInclusive(r.Top, r.Bottom, r.Left, r.Right, rstep, cstep);
    }

    /// <summary>
    /// Get Row-col tuples for each cell in the rectangle
    /// </summary>
    public static IEnumerable<(int r, int c)> RangeInclusive(
        int startRow, int endRow,
        int startCol, int endCol,
        int rstep = DEFAULT_STEP, int cstep = DEFAULT_STEP)
    {
        for (int r = startRow; r <= endRow; r += rstep)
        {
            for (int c = startCol; c <= endCol; c += cstep)
            {
                yield return (r, c);
            }
        }
    }

    /// <summary> Convert to Row-Column Tuple </summary>
    public static (int r, int c) ToRC(this Point p) => (p.Y, p.X);

    /// <summary> Convert from Row-Column Tuple </summary>
    public static Point ToPoint(this (int r, int c) p) => new(x: p.c, y: p.r);
}
