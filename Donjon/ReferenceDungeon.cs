

using System.Diagnostics.CodeAnalysis;

using Donjon.Original;

namespace Donjon;

public class ReferenceDungeon : IDungeon
{
    public required Cellbits[,] cell { get; init; }
    // public required DataField<Cellbits> Field{ get; init; }

    public IDictionary<string, int>? connect { get; set; } = new Dictionary<string, int>();

    public IDictionary<int, IDungeonRoom> room { get; set; } = new Dictionary<int, IDungeonRoom>();

    public IList<DoorData> door { get; set; } = [];

    public IList<StairEnd?> stair { get; set; } = [];

    #region IDungeonDimensional
    private int n_rows => cell.GetLength(0);
    private int n_cols => cell.GetLength(1);
    /// <summary>half rows, will be even by int cast</summary>
    public int n_i => n_rows / 2;

    /// <summary>half cols, will be even by int cast</summary>
    public int n_j => n_cols / 2;

    /// <summary>inclusive-max index of rows (will be even by -1)</summary>
    public int max_row => n_rows - 1;

    /// <summary>inclusive-max index of cols (will be even by -1)</summary>
    public int max_col => n_cols - 1;

    #endregion IDungeonDimensional


    public required IDungeonRoomIssuer RoomSource { get; init; }
    public int n_rooms => RoomSource.n_rooms;
    public int? last_room_id => RoomSource.last_room_id;
    public bool TryIssueRoom([MaybeNullWhen(false), NotNullWhen(true)] out int? id) => RoomSource.TryIssueRoom(out id);

    public static ReferenceDungeon Create(Settings settings, IDungeonRoomIssuer source)
    {
        return new ReferenceDungeon
        {

            cell = new Cellbits[settings.Dungeon.n_rows, settings.Dungeon.n_cols],
            RoomSource = source,
        };
    }
}