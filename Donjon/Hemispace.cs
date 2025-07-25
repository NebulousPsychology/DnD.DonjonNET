using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

using SixLabors.ImageSharp;

namespace Donjon;

public interface ISpacebound<TValue, TSelf> where TSelf : ISpacebound<TValue, TSelf>
{
    public TValue Value { get; init; }
}
interface IImplicitCast<TValue, TSelf> where TSelf : IImplicitCast<TValue, TSelf>
{
    public abstract static implicit operator TValue(TSelf d);
    public abstract static implicit operator TSelf(TValue d);
}
public readonly record struct Realspace<T>(T Value)
    : ISpacebound<T, Realspace<T>>,
    IImplicitCast<T, Realspace<T>>
{
    public static implicit operator T(Realspace<T> d) => d.Value;
    public static implicit operator Realspace<T>(T d) => new(d);
    public readonly override string ToString() => $"{Value}ɾ";


    public static Realspace<TCompare> Min<TCompare>(Realspace<TCompare> a, Realspace<TCompare> b)
    where TCompare : IComparisonOperators<TCompare, TCompare, bool> => new(a.Value <= b.Value ? a.Value : b.Value);
    public static Realspace<TCompare> Max<TCompare>(Realspace<TCompare> a, Realspace<TCompare> b)
    where TCompare : IComparisonOperators<TCompare, TCompare, bool> => new(a.Value >= b.Value ? a.Value : b.Value);

    public static Realspace<Coord> Min(Realspace<Coord> a, Realspace<Coord> b) => new(Coord.Min(a.Value, b.Value));
    public static Realspace<Coord> Max(Realspace<Coord> a, Realspace<Coord> b) => new(Coord.Max(a.Value, b.Value));
}

public readonly record struct Hemispace<T>(T Value)
    : ISpacebound<T, Hemispace<T>>
{
    public static explicit operator T(Hemispace<T> d) => d.Value;
    public readonly override string ToString() => $"{Value}ʜ";

    public static Hemispace<TCompare> Min<TCompare>(Hemispace<TCompare> a, Realspace<TCompare> b)
    where TCompare : IComparisonOperators<TCompare, TCompare, bool> => new(a.Value <= b.Value ? a.Value : b.Value);
    public static Hemispace<TCompare> Max<TCompare>(Hemispace<TCompare> a, Realspace<TCompare> b)
    where TCompare : IComparisonOperators<TCompare, TCompare, bool> => new(a.Value >= b.Value ? a.Value : b.Value);

    public static Hemispace<Coord> Min(Hemispace<Coord> a, Realspace<Coord> b) => new(Coord.Min(a.Value, b.Value));
    public static Hemispace<Coord> Max(Hemispace<Coord> a, Realspace<Coord> b) => new(Coord.Max(a.Value, b.Value));
}

public static class HemispaceConversionExtensions
{
    #region Hemi to Real
    /// <returns>(i * 2 + 1, j * 2 + 1)</returns>
    public static Realspace<T> ToRealspace<T>(this Hemispace<T> proto)
        where T : ISignedNumber<T>, IMultiplyOperators<T, int, T>, IAdditionOperators<T, int, T>
        => new((proto.Value * 2) + 1);
    /// <returns>(i * 2 + 1, j * 2 + 1)</returns>
    public static Realspace<Coord> ToRealspace(this Hemispace<Coord> proto)
        => new((proto.Value * 2) + (1, 1));
    /// <returns>(i * 2 + 1, j * 2 + 1)</returns>
    public static (Realspace<T>, Realspace<T>) ToRealspace<T>(this (Hemispace<T>, Hemispace<T>) proto)
        where T : ISignedNumber<T>, IMultiplyOperators<T, int, T>, IAdditionOperators<T, int, T>
            => (new((proto.Item1.Value * 2) + 1), new((proto.Item2.Value * 2) + 1));
    #endregion Hemi to Real

    #region Real to Hemi
    /// <summary>
    /// condense to the corresponding hemispace (even) coordinate
    /// </summary>
    /// <param name="proto"></param>
    /// <returns></returns>
    public static Hemispace<T> ToHemi<T>(this Realspace<T> proto) where T : ISignedNumber<T>, IDivisionOperators<T, int, T>
        => new(proto.Value / 2);

    /// <summary>
    /// rc->hc: condense to the corresponding hemispace (even) coordinate
    /// </summary>
    /// <param name="proto"></param>
    /// <returns></returns>
    public static Hemispace<Coord> ToHemi(this Realspace<Coord> proto)
        => new(proto.Value / 2);

    /// <summary>
    /// tuple->tuple: condense to the corresponding hemispace (even) coordinate
    /// </summary>
    /// <param name="proto"></param>
    /// <returns></returns>
    public static (Hemispace<T>, Hemispace<T>) ToHemi<T>(this (Realspace<T>, Realspace<T>) proto)
    where T : ISignedNumber<T>, IDivisionOperators<T, int, T>
        => (proto.Item1.ToHemi(), proto.Item2.ToHemi());

    #endregion Real to Hemi

    public static Realspace<Coord> ToRealCoord(this (Hemispace<int>, Hemispace<int>) proto)
        => new((proto.Item1.ToRealspace(), proto.Item2.ToRealspace()));

    public static Hemispace<Coord> ToHemiCoord(this (Realspace<int>, Realspace<int>) proto)
        => proto.ToCoord().ToHemi();


    #region Deconstruct to tuple, construct coord from tuple
    public static Realspace<Coord> ToCoord(this (Realspace<int>, Realspace<int>) proto)
        => new((proto.Item1.Value, proto.Item2.Value));
    public static void Deconstruct(this Realspace<Coord> proto, out Realspace<int> r, out Realspace<int> c)
    {
        r = new(proto.Value.r);
        c = new(proto.Value.c);
    }

    public static Hemispace<Coord> ToCoord(this (Hemispace<int>, Hemispace<int>) proto)
    => new((proto.Item1.Value, proto.Item2.Value));
    public static void Deconstruct(this Hemispace<Coord> proto, out Hemispace<int> r, out Hemispace<int> c)
    {
        r = new(proto.Value.r);
        c = new(proto.Value.c);
    }
    #endregion Deconstruct to tuple, construct coord from tuple

    #region Add/Subtract
    /// <summary>
    /// Add <paramref name="other"/> directly to the internal value of <paramref name="self"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="self"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static Hemispace<T> Add<T>(this Hemispace<T> self, T other) where T : IAdditionOperators<T, T, T>
    => new(self.Value + other);
    /// <summary>
    /// Add <paramref name="other"/> directly to the internal value of <paramref name="self"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="self"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static Realspace<T> Add<T>(this Realspace<T> self, T other) where T : IAdditionOperators<T, T, T>
    => new(self.Value + other);
    #endregion Add/Subtract


    public static IEnumerable<Hemispace<Coord>> AsHemi(this IEnumerable<(int, int)> proto)
        => proto.Select(p => (new Hemispace<int>(p.Item1), new Hemispace<int>(p.Item2)).ToCoord());
    public static IEnumerable<(Realspace<int>, Realspace<int>)> AsRealspace(this IEnumerable<(int, int)> proto)
        => proto.Select(p => (new Realspace<int>(p.Item1), new Realspace<int>(p.Item2)));
#if No
    public static IEnumerable<(Hemispace<int>, Hemispace<int>)> AsHemi(this IEnumerable<(int, int)> proto)
        => proto.Select(p => (new Hemispace<int>(p.Item1), new Hemispace<int>(p.Item2)));
    public static IEnumerable<(Realspace<int>, Realspace<int>)> AsRealspace(this IEnumerable<(int, int)> proto)
        => proto.Select(p => (new Realspace<int>(p.Item1), new Realspace<int>(p.Item2)));
    #region tuple-to-tuple
    /// <summary>
    /// expand hemispace to a realspace point that IS GUARANTEED TO BE ODD
    /// </summary>
    public static (Realspace<int>, Realspace<int>) ToRealspace(this (Hemispace<int>, Hemispace<int>) proto)
        => (new((proto.Item1.Value * 2) + 1), new((proto.Item2.Value * 2) + 1));

    /// <summary>
    /// condense to the corresponding hemispace (even) coordinate
    /// </summary>
    /// <param name="proto"></param>
    /// <returns></returns>
    public static (Hemispace<int>, Hemispace<int>) ToHemi(this (Realspace<int>, Realspace<int>) proto)
        => (new(proto.Item1.Value / 2), new(proto.Item2.Value / 2));
    #endregion tuple-to-tuple
#endif

    #region Point-to-Point
    /// <summary>
    /// a realspace point that IS GUARANTEED TO BE ODD
    /// </summary>
    /// <param name="proto"></param>
    /// <returns></returns>
    public static Realspace<Point> ToRealspace(this Hemispace<Point> proto)
        => new(new Point(
            x: (proto.Value.X * 2) + 1,
            y: (proto.Value.Y * 2) + 1));

    /// <summary>
    /// condense to the corresponding hemispace (even) coordinate
    /// </summary>
    public static Hemispace<Point> ToHemi(this Realspace<Point> proto)
        => new(new Point(
            x: proto.Value.X / 2,
            y: proto.Value.Y / 2));

    #endregion Point-to-Point

    #region Rect-to-Rect
    public static Hemispace<Rectangle> ToHemi(this Realspace<Rectangle> proto)
    {
        //! any odd value will be lost
        return new(new(proto.Value.X / 2, proto.Value.Y / 2, proto.Value.Width / 2, proto.Value.Height / 2));
    }

    public static Realspace<Rectangle> ToRealspace(this Hemispace<Rectangle> proto)
    {
        var x1 = (proto.Value.X * 2) + 1;
        var y1 = (proto.Value.Y * 2) + 1;

        //# room base = (room_min[3] + 1) / 2; = 2
        //# room_radix => (room_max[9] - room_min) / 2 + 1; = 4
        // in alloc_opens: "size"-ish: into hemispace ?
        //::   my $room_h = (($room->{'south'} - $room->{'north'}) / 2) + 1; 
        //! resembles room_radix
        //::   my $room_w = (($room->{'east'} - $room->{'west'}) / 2) + 1;
        //   
        // in emplace room:
        //:: var r2 = ((proto["i"] + proto["height"]) * 2) - 1;
        //:: var c2 = ((proto["j"] + proto["width"]) * 2) - 1;
        //:: ...
        //:: // used to populate IDungeonRoom with realspace cell indices:
        //:: height = ((r2 - r1) + 1) * cellsize,
        //:: width = ((c2 - c1) + 1) * cellsize,
        //? in the case of emplace_room, is the oddityloss is exploited to create perimeter cells?
        // In .NET, Rectangle.Height == 0 means the rectangle covers no rows (empty).
        // Rectangle.Height == 1 means it covers one row.
        // Rectangle.Bottom = Top + Height (exclusive).
        // Rectangle.Right = Left + Width (exclusive).
        (int width, int height) = (proto.Value.Width * 2 - 1, proto.Value.Height * 2 - 1);
        return new Rectangle(x1, y1, width, height);
    }
    #endregion Rect-to-Rect

}
