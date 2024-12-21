using Donjon.ImageTools;

using SixLabors.ImageSharp;

namespace Donjon;

public static class Dim2d
{
    public static IEnumerable<(int r, int c)> RangeUpperExclusive(int startX, int endX, int startY, int endY)
        => RangeInclusive(startX, endX - 1, startY, endY - 1);

    public static IEnumerable<(int r, int c)> RangeInclusive(int startRow, int endRow, int startCol, int endCol)
    {
        for (int r = startRow; r <= endRow; r++)
        {
            for (int c = startCol; c <= endCol; c++)
            {
                yield return new(r, c);
            }
        }
    }

    /// <summary> Convert to Row-Column Tuple </summary>
    public static (int r, int c) ToRC(this Point p) => (p.Y, p.X);
    /// <summary> Convert from Row-Column Tuple </summary>
    public static Point ToPoint(this (int r, int c) p) => new(p.c, p.r);
}

public struct Realspace<T>(T value)
{
    public T Value { get; set; } = value;
    public static implicit operator T(Realspace<T> d) => d.Value;
    public static implicit operator Realspace<T>(T d) => new(d);
}

public struct Hemispace<T>(T value) //where T : System.Numerics.IAdditionOperators<T,T,T>, System.Numerics.IAdditiveIdentity<T,Hemispace<T>>
{
    public T Value { get; set; } = value;
    public static explicit operator T(Hemispace<T> d) => d.Value;
}

public static class HemispaceConversionExtensions
{
    /// <summary>
    /// a realspace point that IS GUARANTEED TO BE ODD
    /// </summary>
    /// <param name="proto"></param>
    /// <returns></returns>
    public static Realspace<Point> ToRealspace(this Hemispace<Point> proto)
    {
        var x = (proto.Value.X * 2) + 1;
        var y = (proto.Value.Y * 2) + 1;
        return new(new(x, y));
    }

    /// <summary>
    /// expand hemispace to a realspace point that IS GUARANTEED TO BE ODD
    /// </summary>
    public static (Realspace<int>, Realspace<int>) ToRealspace(this (Hemispace<int>, Hemispace<int>) proto)
        => (new((proto.Item1.Value * 2) + 1), new((proto.Item2.Value * 2) + 1));

    public static Hemispace<Point> ToHemi(this (Realspace<int>, Realspace<int>) proto)
    => new(new(proto.Item1.Value / 2, proto.Item2.Value / 2));

    public static Hemispace<Point> ToHemi(this Realspace<Point> proto)
    {
        //! any odd value will be lost
        return new(new(proto.Value.X / 2, proto.Value.Y / 2));
    }


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
        (int width, int height) = (proto.Value.Width * 2 + 1, proto.Value.Height * 2 + 1);
        return new Rectangle(x1, y1, width, height);
    }

    // public static Realspace<Size> ToRealspace(this Hemispace<Size> proto)
    // {
    //     (int width, int height) = (proto.Value.Width * 2 +, proto.Value.Height * 2+);
    //     return new Size(width, height);
    // }
}