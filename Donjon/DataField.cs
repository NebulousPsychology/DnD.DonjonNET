
using Donjon.ImageTools;

using SixLabors.ImageSharp;


namespace Donjon;

/// <summary>
/// A 2d array which can reason about accessor actions
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="height"></param>
/// <param name="width"></param>
public class DataField<T>(int height, int width)// needs to be record or class for full indexer
{
    /// <summary>
    /// A data grid of at least 1x1
    /// </summary>
    private T[,] Data { get; } = height > 0 && width > 0 ? new T[height, width]
        : throw new ArgumentOutOfRangeException();
    public int Rows => Data.GetLength(0);
    public int Columns => Data.GetLength(1);

    public int Height => Rows;
    public int Width => Columns;

    /// <summary>
    /// Index of the last physical row
    /// </summary>
    public int FinalRow => Rows - 1;
    /// <summary>
    /// Index of the last physical row
    /// </summary>
    public int FinalColumn => Columns - 1;

    public bool InHeight(int row) => 0 <= row && row < Height;
    public bool InWidth(int column) => 0 <= column && column < Width;

    public SixLabors.ImageSharp.Rectangle Rectangle { get; } = new(0, 0, width, height);
    public SixLabors.ImageSharp.Point Center => Rectangle.Center();
    public SixLabors.ImageSharp.Size Size => Rectangle.Size;

    bool Contains(int row, int column) => Rectangle.Contains(column, row);
    // readonly bool InBounds(int row, int column) => row > 0 && row < Data.GetLength(0) && column > 0 && column < Data.GetLength(1);

    [Obsolete("Use DataField reference directly")] public static implicit operator T[,](DataField<T> d) => d.Data;
    public static implicit operator System.Drawing.Rectangle(DataField<T> d) => new(0, 0, width: d.Width, height: d.Height);
    public static implicit operator SixLabors.ImageSharp.Rectangle(DataField<T> d) => d.Rectangle;

    public T this[int row, int column]
    {
        get => Contains(row, column) ? Data[row, column]
            : throw new ArgumentOutOfRangeException($"({row},{column}) is out of bounds");
        set
        {
            if (!Contains(row, column))
                throw new ArgumentOutOfRangeException($"({row},{column}) is out of bounds");
            Data[row, column] = value;
        }
    }

    public T this[SixLabors.ImageSharp.Point point]
    {
        get => this[point.Y, point.X];
        set => this[point.Y, point.X] = value;
    }

    public static IEnumerable<(int r, int c)> Raster(int height, int width)
        => new RasterEnumerator(height,width,rowMajor:true);

}

// readonly struct UniversalRect(int x, int y, int width, int height)
// {
//     public int X { get; } = x;
//     public int Y { get; } = y;
//     public int Width { get; } = width;
//     public int Height { get; } = height;

//     public static implicit operator UniversalRect(System.Drawing.Rectangle d) => new(d.X, d.Y, d.Width, d.Height);
//     public static implicit operator UniversalRect(SixLabors.ImageSharp.Rectangle d) => new(d.X, d.Y, d.Width, d.Height);

//     public static implicit operator System.Drawing.Rectangle(UniversalRect d) => new(d.X, d.Y, d.Width, d.Height);
//     public static implicit operator SixLabors.ImageSharp.Rectangle(UniversalRect d) => new(d.X, d.Y, d.Width, d.Height);
//     static void x()
//     {
//         // System.Drawing.Rectangle r;r.Size.
//         UniversalRect c = new();
//     }
//     // public static implicit operator System.Drawing.Rectangle(SixLabors.ImageSharp.Rectangle d) => new(0, 0, width: d.Width, height: d.Height);
//     // public static implicit operator SixLabors.ImageSharp.Rectangle(System.Drawing.Rectangle d) => new(0, 0, width: d.Width, height: d.Height);
// }

public static class GridPointExtensions
{
    public static int Row(this Point self) => self.Y;
    public static int Col(this Point self) => self.X;
}
