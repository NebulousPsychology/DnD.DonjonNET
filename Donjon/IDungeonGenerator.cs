using Donjon.Original;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Donjon;

interface IDungeonGenerator
{
    public IDungeon Create_dungeon();
    IDungeonRoomIssuer RoomIssuer { get; }
}

interface IRoomPlacement
{
    public void emplace_rooms();
}

class NuGenerator(IOptions<Settings> settings, ILoggerFactory loggerFactory) : IDungeonGenerator
{
    private ILogger<NuGenerator> Logger { get; } = loggerFactory?.CreateLogger<NuGenerator>()
        ?? NullLogger<NuGenerator>.Instance;
    public IDungeonRoomIssuer RoomIssuer { get; } = new RoomIdIssuer(Options.Create(settings.Value.Rooms), Options.Create(settings.Value.Dungeon));
    public IDungeon Create_dungeon()
    {
        throw new NotImplementedException();
    }
}

class OldGeneratorWrapper(IOptions<Settings> settings, ILoggerFactory loggerFactory) : IDungeonGenerator
{
    private ILogger<OldGeneratorWrapper> Logger { get; } = loggerFactory?.CreateLogger<OldGeneratorWrapper>() ?? NullLogger<OldGeneratorWrapper>.Instance;

    Dungeon d = new()
    {
        seed = settings.Value.seed,
        n_rows = settings.Value.Dungeon.n_rows,
        n_cols = settings.Value.Dungeon.n_cols,
        dungeon_layout = settings.Value.Dungeon.dungeon_layout,
        room_min = settings.Value.Rooms.room_min,
        room_max = settings.Value.Rooms.room_max,
        room_layout = settings.Value.Rooms.room_layout,
        corridor_layout = settings.Value.Corridors.corridor_layout,
        remove_deadends = settings.Value.Corridors.remove_deadends,
        add_stairs = settings.Value.Corridors.add_stairs,
        map_style = settings.Value.Map.map_style,
        cell_size = settings.Value.Map.cell_size,
        n_rooms = 0,
        last_room_id = null,
        // cell = new Lazy<Cellbits[,]>(valueFactory: () => new Cellbits[n_rows, n_cols]),
        // _random = new Lazy<Random>(valueFactory: () => new Random(Seed: (int)seed)),
    };
    public IDungeonRoomIssuer RoomIssuer => d;

    DungeonGen generator = new DungeonGen(loggerFactory?.CreateLogger<DungeonGen>() ?? NullLogger<DungeonGen>.Instance);

    public IDungeon Create_dungeon()
    {
        d = generator.Create_dungeon(d);
        return d;
    }
}
#pragma warning restore IDE1006 // Naming Styles
