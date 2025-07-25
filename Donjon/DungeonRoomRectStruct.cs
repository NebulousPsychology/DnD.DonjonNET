using System.Diagnostics;
using System.Runtime.CompilerServices;

using Donjon.Original;

using SixLabors.ImageSharp;
namespace Donjon;

[DebuggerDisplay("{id}, ({north},{west})..({south},{east}), a={area}, {door.Count}door")]
public struct DungeonRoomRectStruct : IDungeonRoom
{
    public required Dictionary<Cardinal, List<DoorData>> door { get; init; }
    public int id { get; init; }
    public required Realspace<Rectangle> Rectangle
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            try
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value.Value.Height, 1, nameof(value.Value.Height));
                ArgumentOutOfRangeException.ThrowIfLessThan(value.Value.Width, 1, nameof(value.Value.Width));
            }
            catch (ArgumentOutOfRangeException aoor)
            {
                throw new Exception("Rectangles msut have H&W > 0 to be a meaningful room", innerException: aoor);
            }
            field = value;
        }
    }

    public readonly int north => Rectangle.Value.Top;
    public readonly int south => Rectangle.Value.Bottom - 1;//? Bottom is Y+Height, so index of south is bottom-1,min Y
    public readonly int east => Rectangle.Value.Right - 1;//? Right is X+Width, so index of east is right-1, min X
    public readonly int west => Rectangle.Value.Left;
    public readonly int row => Rectangle.Value.Y;
    public readonly int col => Rectangle.Value.X;
    public readonly int height => Rectangle.Value.Height;
    public readonly int width => Rectangle.Value.Width;
    public readonly int area => height * width;

    public readonly bool Equals(IDungeonRoom? other)
    {
        return other is not null &&
            other.id == id &&
            other.north == north &&
            other.south == south &&
            other.east == east &&
            other.west == west &&
            other.row == row &&
            other.col == col &&
            other.height == height &&
            other.width == width &&
            other.area == area &&
            other.Perimeter == (this as IDungeonRoom).Perimeter &&
            other.id == id &&
            true;
    }
}

