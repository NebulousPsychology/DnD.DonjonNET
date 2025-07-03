
using System.Collections;

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
    private readonly Func<int, int, bool> inbounds = inclusive ? (a, b) => a <= b : (a, b) => a < b;

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

    public bool MoveNext()
    {
#if true 
        (_current, var ret) = rowMajor switch
        {
            true when inbounds(_current.c + step, Max.c) => ((r: _current.r, c: _current.c + step), true),
            true when !inbounds(_current.c + step, Max.c) && inbounds(_current.r + step, Max.r)
                => ((_current.r + step, c: Min.c), true),

            false when inbounds(_current.r + step, Max.r) => ((r: _current.r + step, c: _current.c), true),
            false when !inbounds(_current.r + step, Max.r) && inbounds(_current.c + step, Max.c)
                => ((Min.r, c: _current.c + step), true),

            _ => (_current, false),
        };
        return ret;
#else
        if (rowMajor)
        {
            // if raster can get away with the minor only...
            if (inbounds(_current.c + 1, Max.c)) { _current.c++; return true; }
            // otherwise, if raster can EOL
            else if (inbounds(_current.r + 1, Max.r))
            {
                _current = (r: _current.r + 1, c: _start.c); return true;
            }
            // or, just stop
            else { return false; }
        }
        else
        {

            if (inbounds(Current.r + 1, Max.r))
            {
                _current.r++; return true;
            }
            else if (inbounds(Current.c + 1, Max.c))
            {
                _current = (r: _start.r, c: _current.c + 1);
                return true;
            }
            else { return false; }
        }
#endif
    }

    public IEnumerator<(int r, int c)> GetEnumerator() => this;

    IEnumerator IEnumerable.GetEnumerator() => this;
}
