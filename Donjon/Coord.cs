using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Donjon.Original;

using SixLabors.ImageSharp;

namespace Donjon;

/// <summary>
/// todo: consider [MethodImpl(MethodImplOptions.AggressiveInlining)] and friends
/// </summary>
public struct Coord()
: System.Numerics.IAdditionOperators<Coord, Coord, Coord>,
    System.Numerics.ISubtractionOperators<Coord, Coord, Coord>,
    System.Numerics.IUnaryNegationOperators<Coord, Coord>,
    System.Numerics.IMultiplyOperators<Coord, int, Coord>,
    System.Numerics.IDivisionOperators<Coord, int, Coord>,
    System.Numerics.IEqualityOperators<Coord, Coord, bool>
{
    public int r { get; set; } = 0;
    public int c { get; set; } = 0;
    public readonly override string ToString() => $"({r},{c})";

    public static bool operator ==(Coord left, Coord right) => left.r == right.r && left.c == right.c;
    public static bool operator !=(Coord left, Coord right) => (left == right) is false;
    public static Coord operator +(Coord left, Coord right) => new() { r = left.r + right.r, c = left.c + right.c };
    public static Coord operator -(Coord value) => value * -1;
    public static Coord operator -(Coord left, Coord right) => new() { r = left.r - right.r, c = left.c - right.c };
    public static Coord operator *(Coord left, int right) => new() { r = left.r * right, c = left.c * right };
    public static Coord operator *(int left, Coord right) => new() { r = right.r * left, c = right.c * left };
    public static Coord operator /(Coord left, int right) => new() { r = left.r / right, c = left.c / right };

    public static implicit operator (int, int)(Coord me) => (me.r, me.c);
    public static implicit operator Coord((int, int) p) => new() { r = p.Item1, c = p.Item2 };


    public static implicit operator Size(Coord me) => new(height: me.r, width: me.c);
    public static implicit operator Point(Coord me) => new(x: me.c, y: me.r);
    public static implicit operator Coord(Point p) => new() { r = p.Y, c = p.X };

    public static implicit operator Cardinal(Coord me) => me switch
    {
        Coord x when x == Coord.East => Cardinal.east,
        Coord x when x == Coord.West => Cardinal.west,
        Coord x when x == Coord.North => Cardinal.north,
        Coord x when x == Coord.South => Cardinal.south,
        _ => throw new ArgumentException(),
    };
    public static implicit operator Coord(Cardinal p) => p switch
    {
        Cardinal.east => East,
        Cardinal.west => West,
        Cardinal.north => North,
        Cardinal.south => South,
        _ => throw new ArgumentException(),
    };

    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Coord other ? this == other : base.Equals(obj);
    }
    public override readonly int GetHashCode() => base.GetHashCode();

    public readonly static Coord Zero = (0, 0);
    /// <summary> -1, 0 </summary>
    public readonly static Coord North = (-1, 0);
    /// <summary> +1, 0 </summary>
    public readonly static Coord South = (1, 0);
    /// <summary> 0, +1 </summary>
    public readonly static Coord East = (0, 1);
    /// <summary> 0, -1 </summary>
    public readonly static Coord West = (0, -1);
    /// <summary> -1, -1 </summary>
    public readonly static Coord NorthWest = North + West;
    /// <summary> -1, +1 </summary>
    public readonly static Coord NorthEast = North + East;
    /// <summary> +1, -1 </summary>
    public readonly static Coord SouthWest = South + West;
    /// <summary> +1, +1 </summary>
    public readonly static Coord SouthEast = South + East;

    public static Coord Min(Coord a, Coord b) => new() { c = Math.Min(a.c, b.c), r = Math.Min(a.r, b.r) };
    public static Coord Max(Coord a, Coord b) => new() { c = Math.Max(a.c, b.c), r = Math.Max(a.r, b.r) };
}

public static class CoordExtensions
{
    public static T Get<T>(this T[,] grid, Coord c) => grid[c.r, c.c];
    public static void Set<T>(this T[,] grid, Coord c, T value) => grid[c.r, c.c] = value;
    public static void Set<T>(this T[,] grid, Coord c, Func<T, T> operation) => grid[c.r, c.c] = operation(grid[c.r, c.c]);
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="grid"></param>
    /// <param name="c"></param>
    /// <param name="predicate"></param>
    /// <param name="operation"></param>
    /// <returns>the predicate outcome</returns>
    public static bool SetIf<T>(this T[,] grid, Coord c, Predicate<T> predicate, Func<T, T> operation)
    {
        var condition = predicate(grid[c.r, c.c]);
        if (condition) grid[c.r, c.c] = operation(grid[c.r, c.c]);
        return condition;
    }

    public static bool Contains(this IDungeonDimensional dims, Hemispace<Coord> coord) =>
        coord.Value.r >= 0 && coord.Value.r <= dims.n_i &&
        coord.Value.c >= 0 && coord.Value.c <= dims.n_j;
    public static bool Contains(this IDungeonDimensional dims, Realspace<Coord> coord) =>
        coord.Value.r >= 0 && coord.Value.r <= dims.max_row &&
        coord.Value.c >= 0 && coord.Value.c <= dims.max_col;
    public static Rectangle Rectangle(this IDungeonDimensional dims)
        => new(0, 0, width: dims.max_col + 1, height: dims.max_row + 1);
    public static bool Contains(this IDungeonDimensional dims, Realspace<Rectangle> coord)
        => dims.Rectangle().Contains(coord.Value);

}
