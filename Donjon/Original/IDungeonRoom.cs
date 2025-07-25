// Adapted from https://donjon.bin.sh/code/dungeon/dungeon.pl
// https://creativecommons.org/licenses/by-nc/3.0/
namespace Donjon.Original;
#pragma warning disable IDE1006 // Naming Styles

public interface IDungeonRoom : IEquatable<IDungeonRoom>
{
    public int id { get; }
    /// <summary>
    /// index of The North-most row enclosed by the room
    /// </summary>
    public int north { get; }
    /// <summary>
    /// index of The South-most row enclosed by the room
    /// </summary>
    public int south { get; }
    /// <summary>
    /// index of The East-most Col enclosed by the room
    /// </summary>
    public int east { get; }
    /// <summary>
    /// index of The West-most Col enclosed by the room
    /// </summary>
    public int west { get; }
    /// <summary>
    /// index of Starting Row enclosed
    /// </summary>
    public int row { get; }
    /// <summary>
    /// index of starting col enclosed
    /// </summary>
    public int col { get; }

    /// <summary>
    /// Count of rows.
    /// <br/> A rectangle at (0,0) with Height=1, Width=1 covers only cell (0,0).
    /// <br/> Height&lt;1 is meaningless as a room
    /// </summary>
    public int height { get; }

    /// <summary>
    /// Count of columns.
    /// <br/> A rectangle at (0,0) with Height=1, Width=1 covers only cell (0,0).
    /// <br/> Width&lt;1 is meaningless as a room
    /// </summary>
    public int width { get; }

    public int area => height * width;
    public int Perimeter => 2 * (height + width);
    public Dictionary<Cardinal, List<DoorData>> door { get; }

    public bool Equals(IDungeonRoom? other)
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
            other.Perimeter == Perimeter &&
            other.id == id &&
            true;
    }
}
#pragma warning restore IDE1006 // Naming Styles
