
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Donjon;

public interface IDungeonGenerator //: IDungeonDescriber
{
    public IDungeon Create_dungeon();
    protected IDungeonRoomIssuer RoomIssuer { get; }
}

public interface IDungeonDescriber<TOutput>
{
    public TOutput DescribeDungeonLite(IDungeon dungeon);
}

interface IRoomPlacement
{
    public void emplace_rooms();
}

/// <summary>
/// An adapter to use the original 1:1 implementation
/// </summary>
/// <param name="settings"></param>
/// <param name="loggerFactory"></param>
public class OriginalGeneratorAdapter(IOptions<Settings> settings, ILoggerFactory loggerFactory)
    : IDungeonGenerator
{
    private ILogger<OriginalGeneratorAdapter> Logger { get; }
        = loggerFactory?.CreateLogger<OriginalGeneratorAdapter>()
        ?? NullLogger<OriginalGeneratorAdapter>.Instance;

    public static Original.Dungeon CreateLegacy(Settings s) => new()
    {
        seed = s.seed,
        n_rows = s.Dungeon.n_rows,
        n_cols = s.Dungeon.n_cols,
        dungeon_layout = s.Dungeon.dungeon_layout,
        room_min = s.Rooms.room_min,
        room_max = s.Rooms.room_max,
        room_layout = s.Rooms.room_layout,
        corridor_layout = s.Corridors.corridor_layout,
        remove_deadends = s.Corridors.remove_deadends,
        add_stairs = s.Corridors.add_stairs,
        map_style = s.Map.map_style,
        cell_size = s.Map.cell_size,
    };
    OriginalDungeonAdapter d = new(CreateLegacy(settings.Value));
    public IDungeonRoomIssuer RoomIssuer => d;

    Original.DungeonGen Generator { get; } = new(loggerFactory?
        .CreateLogger<Original.DungeonGen>()
        ?? NullLogger<Original.DungeonGen>.Instance);

    public IDungeon Create_dungeon()
    {
        d = new OriginalDungeonAdapter(Generator.Create_dungeon(d.Data));
        return d;
    }

    public class OriginalDungeonAdapter(Original.Dungeon data) : IDungeon
    {
        public Original.Dungeon Data { get; } = data;

        public Original.Cellbits[,] cell => Data.cell;

        public Dictionary<string, int>? connect => Data.connect;

        public Dictionary<int, Original.IDungeonRoom> room => Data.room.ToDictionary(
            keySelector: o => o.Key is int k ? k : o.Key.GetHashCode(),
            elementSelector: o => o.Value);

        public List<Original.DoorData> door => Data.door;

        public List<Original.StairEnd?> stair => Data.stair;

        public int n_i => Data.n_i;

        public int n_j => Data.n_j;

        public int max_row => Data.max_row;

        public int max_col => Data.max_col;

        public int n_rooms => Data.n_rooms;

        public int? last_room_id => Data.last_room_id;

        public bool TryIssueRoom(out int id)
        {
            Data.last_room_id = n_rooms;
            id = ++Data.n_rooms;
            return true;
        }

    }
}
#pragma warning restore IDE1006 // Naming Styles
