// Adapted from https://donjon.bin.sh/code/dungeon/dungeon.pl
// https://creativecommons.org/licenses/by-nc/3.0/
namespace Donjon;
#pragma warning disable IDE1006 // Naming Styles

public record Opts
{
    public long seed = DateTime.Now.Ticks;

    /// <remarks>must be odd</remarks
    public int n_rows { get; init { ArgumentOutOfRangeException.ThrowIfZero(value % 2); field = value; } } = 39;          // must be an odd number

    /// <summary>
    /// the odd number of columns
    /// </summary>
    public int n_cols { get; init { ArgumentOutOfRangeException.ThrowIfZero(value % 2); field = value; } } = 39;          // must be an odd number


    /// <see cref="DungeonGen.dungeon_layout"/>
    public string dungeon_layout = "None"; // box/cross

    ///<summary>minimum room size</summary>
    public int room_min = 3;

    ///<summary> maximum room size</summary>
    public int room_max = 9;

    public RoomLayout room_layout = RoomLayout.Scattered;

    /// <summary>Bent, Labyrinth, or Straight</summary>
    /// <see cref="DungeonGen.corridor_layout"/>
    /// <see cref="CorridorLayout"/>
    public string corridor_layout = "Bent"; // or labyrinth, or straight

    /// <summary>percent of deadends to remove</summary>
    public double remove_deadends = 50;

    /// <summary> number of stairs</summary>
    public int add_stairs = 2;

    public string map_style = "Standard";
    /// <summary>
    /// cell size, in pixels? (image gen)
    /// </summary>
    public int cell_size = 18;
}

#pragma warning restore IDE1006 // Naming Styles
