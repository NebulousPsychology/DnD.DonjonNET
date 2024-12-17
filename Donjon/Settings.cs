namespace Donjon;
#pragma warning disable IDE1006 // Naming Styles

public class Settings
{
    public long seed { get; set; }
}
public class DungeonSettings
{
    /// <remarks>must be odd</remarks
    public int n_rows { get; init { ArgumentOutOfRangeException.ThrowIfZero(value % 2); field = value; } } = 39;

    /// <summary>
    /// the odd number of columns
    /// </summary>
    public int n_cols { get; init { ArgumentOutOfRangeException.ThrowIfZero(value % 2); field = value; } } = 39;

    /// <see cref="DungeonGen.dungeon_layout"/>
    public string dungeon_layout = "None"; // box/cross also "round" procedurally

    enum Layout { None, Box, Cross }

}

public class RoomSettings
{
    ///<summary>minimum room size</summary>
    public int room_min = 3;

    ///<summary> maximum room size</summary>
    public int room_max = 9;
    public Original.RoomLayout room_layout = Original.RoomLayout.Scattered;
}


public class CorridorSettings
{

    /// <summary>Bent, Labyrinth, or Straight</summary>
    /// <see cref="DungeonGen.corridor_layout"/>
    /// <see cref="CorridorLayout"/>
    public string corridor_layout = "Bent"; // or labyrinth, or straight

    /// <summary>percent of deadends to remove</summary>
    public double remove_deadends = 50;

    /// <summary> number of stairs</summary>
    public int add_stairs = 2;

}


public class MapSettings
{
    public string map_style = "Standard";
    /// <summary>
    /// cell size, in pixels? (image gen)
    /// </summary>
    public int cell_size = 18;
}

#pragma warning restore IDE1006 // Naming Styles
