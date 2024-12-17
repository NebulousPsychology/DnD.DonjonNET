namespace Donjon;

using Donjon.Original;

using TRoomId = int;

#pragma warning disable IDE1006 // Naming Styles
public interface IDungeon : IDungeonDimensional, IDungeonRoomIssuer
{
    public Cellbits[,] cell { get; }
    public Dictionary<string, int>? connect { get; }
    public Dictionary<TRoomId, IDungeonRoom> room { get; }
    public List<DoorData> door { get; }
    public List<StairEnd?> stair { get; }
}

public interface IDungeonDimensional
{
    /// <summary>half rows, will be even by intcast</summary>
    public int n_i { get; }

    /// <summary>half cols, will be even by intcast</summary>
    public int n_j { get; }

    /// <summary>inclusive-max index of rows (will be even by -1)</summary>
    public int max_row { get; }

    /// <summary>inclusive-max index of cols (will be even by -1)</summary>
    public int max_col { get; }

    /// <summary> (room_min[3] + 1) / 2 </summary>
    public int room_base { get; }

    /// <summary> (room_max[9] - room_min[3]) / 2 + 1 </summary>
    public int room_radix { get; }
}
