using Donjon.Original;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Donjon;

interface IDungeonGenerator //: IDungeonDescriber
{
    public IDungeon Create_dungeon();
    protected IDungeonRoomIssuer RoomIssuer { get; }
}

interface IDungeonDescriber<TOutput>
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
class OldGeneratorWrapper(IOptions<Settings> settings, ILoggerFactory loggerFactory) : IDungeonGenerator
{
    private ILogger<OldGeneratorWrapper> Logger { get; } = loggerFactory?.CreateLogger<OldGeneratorWrapper>() ?? NullLogger<OldGeneratorWrapper>.Instance;

    Dungeon d = Settings.CreateLegacy(settings.Value);
    // Dungeon d = new()
    // {
    //     seed = settings.Value.seed,
    //     n_rows = settings.Value.Dungeon.n_rows,
    //     n_cols = settings.Value.Dungeon.n_cols,
    //     dungeon_layout = settings.Value.Dungeon.dungeon_layout,
    //     room_min = settings.Value.Rooms.room_min,
    //     room_max = settings.Value.Rooms.room_max,
    //     room_layout = settings.Value.Rooms.room_layout,
    //     corridor_layout = settings.Value.Corridors.corridor_layout,
    //     remove_deadends = settings.Value.Corridors.remove_deadends,
    //     add_stairs = settings.Value.Corridors.add_stairs,
    //     map_style = settings.Value.Map.map_style,
    //     cell_size = settings.Value.Map.cell_size,
    //     n_rooms = 0,
    //     last_room_id = null,
    //     // cell = new Lazy<Cellbits[,]>(valueFactory: () => new Cellbits[n_rows, n_cols]),
    //     // _random = new Lazy<Random>(valueFactory: () => new Random(Seed: (int)seed)),
    // };
    public IDungeonRoomIssuer RoomIssuer => d;

    DungeonGen Generator { get; } = new(loggerFactory?.CreateLogger<DungeonGen>() ?? NullLogger<DungeonGen>.Instance);

    public IDungeon Create_dungeon()
    {
        d = Generator.Create_dungeon(d);
        return d;
    }

    public string DescribeDungeonLite(IDungeon dungeon) => Generator.DescribeDungeonLite(dungeon);
}
#pragma warning restore IDE1006 // Naming Styles
