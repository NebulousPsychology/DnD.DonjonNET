using System.Text.Json;

namespace Donjon;
#pragma warning disable IDE1006 // Naming Styles

public class Settings
{
    public long seed { get; set; }
    public required DungeonSettings Dungeon { get; init; }
    public required RoomSettings Rooms { get; init; }
    public required CorridorSettings Corridors { get; init; }
    public required MapSettings Map { get; init; }
}

public class DungeonSettings : IDungeonDimensional
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

    #region IDungeonDimensional
    /// <summary>half rows, will be even by intcast</summary>
    public int n_i => n_rows / 2;

    /// <summary>half cols, will be even by intcast</summary>
    public int n_j => n_cols / 2;

    /// <summary>inclusive-max index of rows (will be even by -1)</summary>
    public int max_row => n_rows - 1;

    /// <summary>inclusive-max index of cols (will be even by -1)</summary>
    public int max_col => n_cols - 1;
    #endregion IDungeonDimensional
}

public class RoomSettings
{
    ///<summary>minimum room size</summary>
    public int room_min = 3;

    ///<summary> maximum room size</summary>
    public int room_max = 9;

    public Original.RoomLayout room_layout = Original.RoomLayout.Scattered;

    /// <summary> (room_min[3] + 1) / 2 </summary>
    public int room_base => (room_min + 1) / 2;

    /// <summary> (room_max[9] - room_min[3]) / 2 + 1 </summary>
    public int room_radix => (room_max - room_min) / 2 + 1;
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

public static class JsonExtensions
{
    readonly static JsonSerializerOptions Indented = new(JsonSerializerDefaults.General) { WriteIndented = true };
    readonly static JsonSerializerOptions Default = new(JsonSerializerDefaults.General);
    public static string ToJson<T>(this T self, bool indent = false) => JsonSerializer.Serialize(self, indent ? Indented : Default);
}
