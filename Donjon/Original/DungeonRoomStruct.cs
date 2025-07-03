// Adapted from https://donjon.bin.sh/code/dungeon/dungeon.pl
// https://creativecommons.org/licenses/by-nc/3.0/
using System.Diagnostics;

namespace Donjon.Original;
#pragma warning disable IDE1006 // Naming Styles

[DebuggerDisplay("{id}, ({north},{west})..({south},{east}), a={area}, {door.Count}door")]
public struct DungeonRoomStruct : IDungeonRoom
{
    public required Dictionary<Cardinal, List<DoorData>> door { get; init; }
    public int id { get; init; }
    public int north { get; init; }
    public int south { get; init; }
    public int east { get; init; }
    public int west { get; init; }
    public int row { get; init; }
    public int col { get; init; }
    public int height { get; init; }
    public int width { get; init; }
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

#pragma warning restore IDE1006 // Naming Styles
