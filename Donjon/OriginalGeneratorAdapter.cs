
using System.Collections;
using System.Diagnostics.CodeAnalysis;

using Donjon.Original;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Donjon;

/// <summary>
/// An adapter to use the original 1:1 implementation
/// </summary>
/// <param name="settings"></param>
/// <param name="loggerFactory"></param>
public class OriginalGeneratorAdapter(IOptions<Settings> settings, ILoggerFactory loggerFactory)
    : IDungeonGenerator
{
    public static OriginalDungeonAdapter CreateLegacy(Settings s) => new(OriginalDungeonAdapter.CreateLegacy(s));
    private ILogger<OriginalGeneratorAdapter> Logger { get; }
        = loggerFactory?.CreateLogger<OriginalGeneratorAdapter>()
        ?? NullLogger<OriginalGeneratorAdapter>.Instance;

    OriginalDungeonAdapter d = CreateLegacy(settings.Value);
    public IDungeonRoomIssuer RoomIssuer => d;

    Original.DungeonGen Generator { get; } = new(loggerFactory?
        .CreateLogger<Original.DungeonGen>()
        ?? NullLogger<Original.DungeonGen>.Instance);

    public IDungeon Create_dungeon()
    {
        d = new OriginalDungeonAdapter(Generator.Create_dungeon(d.Data));
        return d;
    }
}

/// <summary>
/// Allows Original.Dungeon to act as an IDungeon
/// </summary>
/// <param name="data"></param>
public class OriginalDungeonAdapter(Original.Dungeon data) : IDungeon
{
    public static Original.Dungeon CreateLegacy(Settings s) => new()
    {
        seed = s.seed,
        n_rows = s.Dungeon.n_rows,
        n_cols = s.Dungeon.n_cols,
        dungeon_layout = s.Dungeon.dungeon_layout,
        room_min = s.Rooms.room_min,
        room_max = s.Rooms.room_max,
        room_layout = s.Rooms.room_layout,
        corridor_layout = s.Corridors.corridor_layout.ToString(),
        remove_deadends = s.Corridors.remove_deadends,
        add_stairs = s.Corridors.add_stairs,
        map_style = s.Map.map_style,
        cell_size = s.Map.cell_size,
    };
    public Original.Dungeon Data { get; } = data;

    public Original.Cellbits[,] cell => Data.cell;

    public IDictionary<string, int>? connect => Data.connect;
    public class IntRoomDictionaryAdapter(IDictionary<object, IDungeonRoom> _inner) : IDictionary<int, Original.IDungeonRoom>
    {

        public IDungeonRoom this[int key] { get => _inner[key]; set => _inner[key] = value; }

        public ICollection<int> Keys => _inner.Keys.Cast<int>().ToList();

        public ICollection<IDungeonRoom> Values => _inner.Values;

        public int Count => _inner.Count;

        public bool IsReadOnly => _inner.IsReadOnly;

        public void Add(int key, IDungeonRoom value) => _inner.Add(key, value);

        public void Add(KeyValuePair<int, IDungeonRoom> item) => _inner.Add(new(item.Key, item.Value));

        public void Clear() => _inner.Clear();

        public bool Contains(KeyValuePair<int, IDungeonRoom> item) => _inner.Contains(new(item.Key, item.Value));

        public bool ContainsKey(int key) => _inner.ContainsKey(key);

        public void CopyTo(KeyValuePair<int, IDungeonRoom>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<int, IDungeonRoom>> GetEnumerator()
        {
            foreach (var kvp in _inner)
            {
                if (kvp.Key is int k)
                    yield return new KeyValuePair<int, Original.IDungeonRoom>(k, kvp.Value);
            }
        }

        public bool Remove(int key) => _inner.Remove(key);
        public bool Remove(KeyValuePair<int, IDungeonRoom> item) => _inner.Remove(item.Key);

        public bool TryGetValue(int key, [MaybeNullWhen(false)] out IDungeonRoom value)
        => _inner.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private readonly IntRoomDictionaryAdapter roomWrapper = new(data.room);
    //FIXME: unable to add rooms with this derivation!
    public IDictionary<int, Original.IDungeonRoom> room => roomWrapper;

    public IList<Original.DoorData> door => Data.door;

    public IList<Original.StairEnd?> stair => Data.stair;

    public int n_i => Data.n_i;

    public int n_j => Data.n_j;

    public int max_row => Data.max_row;

    public int max_col => Data.max_col;

    public int n_rooms => Data.n_rooms;

    public int? last_room_id => Data.last_room_id;

    public bool TryIssueRoom([MaybeNullWhen(false), NotNullWhen(true)] out int? id)
    {
        Data.last_room_id = n_rooms;
        id = ++Data.n_rooms;
        return true;
    }
}
#pragma warning restore IDE1006 // Naming Styles

/// <summary>
/// Original.DungeonGen adapter, by extension
/// </summary>
/// <param name="settings"></param>
/// <param name="loggerFactory"></param>
public class OriGeneratorPipelineAdapter(
    IOptions<Settings> settings,
    ILoggerFactory loggerFactory)
    : DungeonGen(loggerFactory.CreateLogger<DungeonGen>()), IDungeonGenerator
{
    ILogger<OriGeneratorPipelineAdapter> Logger { get; } = loggerFactory.CreateLogger<OriGeneratorPipelineAdapter>();

    public Original.Dungeon Data { get; init; } = OriginalDungeonAdapter.CreateLegacy(settings.Value);
    IDungeonRoomIssuer IDungeonGenerator.RoomIssuer => new OriginalIssuer(this);

    class OriginalIssuer(OriGeneratorPipelineAdapter source) : IDungeonRoomIssuer
    {
        public int n_rooms => source.Data.n_rooms;

        public int? last_room_id => source.Data.last_room_id;

        public bool TryIssueRoom([MaybeNullWhen(false), NotNullWhen(true)] out int? id)
        {
            source.Data.last_room_id = n_rooms;
            id = ++source.Data.n_rooms;
            return true;
        }
    }

    public IDungeon Create_dungeon()
    {
        using (Logger.BeginScope(nameof(Create_dungeon)))
        {
            var p = new Pipeline.Pipeline<Dungeon>(loggerFactory.CreateLogger<Pipeline.Pipeline<Dungeon>>())
            .RegisterStep(init_cells)
            .RegisterStep(emplace_rooms)
            // kiwi
            .RegisterStep(open_rooms)
            // grape
            .RegisterStep(label_rooms)
            //orange
            .RegisterStep(corridors)
            // ! GOOD TO HERE <<<<
            // apple
            .RegisterStep(dungeon =>
            {
                if (dungeon.add_stairs != 0)
                {
                    dungeon = emplace_stairs(dungeon);
                    Logger.LogInformation(5, "dungeonState {s}", DescribeDungeonLite(dungeon));
                }
                return dungeon;
            })
            // pear
            .RegisterStep(clean_dungeon)
#if DISABLE_PARITY_SHORTCIRCUIT
#endif
            ;

            return p.TryInvoke(Data, out var d)
            ? new OriginalDungeonAdapter(d)
            : throw new Exception();
        }
    }
}
