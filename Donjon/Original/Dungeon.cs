// Adapted from https://donjon.bin.sh/code/dungeon/dungeon.pl
// https://creativecommons.org/licenses/by-nc/3.0/
namespace Donjon.Original;
#pragma warning disable IDE1006 // Naming Styles

public record Dungeon : Opts
{
    #region IDungeonRoomIssuer
    /// <summary>number of room_ids issued (and the source counter for issuing them)  </summary>
    public int n_rooms { get; set; } = 0; //? should this be a member of opts?

    /// <summary>last room_id issued</summary>
    public int? last_room_id { get; set; } = null;
    #endregion IDungeonRoomIssuer

    #region IDungeonDimensional
    /// <summary>half rows, will be even by int cast</summary>
    public int n_i => n_rows / 2;

    /// <summary>half cols, will be even by int cast</summary>
    public int n_j => n_cols / 2;

    /// <summary>inclusive-max index of rows (will be even by -1)</summary>
    public int max_row => n_rows - 1;

    /// <summary>inclusive-max index of cols (will be even by -1)</summary>
    public int max_col => n_cols - 1;

    #endregion IDungeonDimensional

    #region RoomSettings-derived
    /// <summary> (room_min[3] + 1) / 2 </summary>
    public int room_base => (room_min + 1) / 2;

    /// <summary> (room_max[9] - room_min[3]) / 2 + 1 </summary>
    public int room_radix => (room_max - room_min) / 2 + 1;
    #endregion RoomSettings-derived

    #region IDungeon
    private readonly Lazy<Cellbits[,]> _cellbits;
    [System.Text.Json.Serialization.JsonIgnore]
    public Cellbits[,] cell => _cellbits.Value;
    public Lazy<Random> _random { get; private set; }
    public Random random => _random.Value;
    public Dictionary<string, int>? connect { get; private set; } = [];
    public Dictionary<object, IDungeonRoom> room { get; private set; } = [];
    public List<DoorData> door { get; private set; } = [];
    public List<StairEnd?> stair { get; private set; } = [];
    #endregion IDungeon

    public Dungeon() : this(new Opts())
    {
    }

    protected Dungeon(Opts original) : base(original)
    {
        if (n_cols % 2 == 0) throw new InvalidOperationException($"{nameof(n_cols)} must be odd");
        if (n_rows % 2 == 0) throw new InvalidOperationException($"{nameof(n_rows)} must be odd");
        (room_min, room_max) = room_min <= room_max ? (room_min, room_max) : (room_max, room_min);
        _cellbits = new Lazy<Cellbits[,]>(valueFactory: () => new Cellbits[n_rows, n_cols]);
        _random = new Lazy<Random>(valueFactory: () => new Random(Seed: (int)seed));
    }

    private static void ForeachBase(
        Action<int, int> action,
        Func<int, bool> rowlimit,
        Func<int, bool> collimit,
        int rowStart = 0, int colStart = 0
        )
    {
        for (int r = rowStart; rowlimit(r); r++)
        {
            for (int c = colStart; collimit(c); c++)
            {
                action(r, c);
            }
        }
    }

    /// <summary>
    /// A row-major square loop across every actual cell in the <see cref="cell"/> grid
    /// </summary>
    /// <param name="action"></param>
    public void Foreach(Action<int, int> action)
        => ForeachBase(action, r => r < cell.GetLength(0), c => c < cell.GetLength(1));

    /// <summary>
    /// A row-major square loop across 0,0 .. <see cref="max_row"/>, <see cref="max_col"/>
    /// </summary>
    /// <param name="action"></param>
    public void ForeachInclusive(Action<int, int> action)
        => ForeachBase(action, r => r <= max_row, c => c <= max_col);

    /// <summary>
    /// A row-major square loop across 0,0 .. <see cref="n_rows"/>, <see cref="n_cols"/>
    /// </summary>
    /// <remarks> THIS SHOULDN"T WORK, EXCEPT IN A +1,+1 pad
    /// <param name="action"></param>
    public void ForeachInclusiveN(Action<int, int> action)
        => ForeachBase(action, r => r <= n_rows, c => c <= n_cols);

    /// <summary>
    /// A row-major square loop across every actual cell in the <see cref="cell"/> grid
    /// </summary>
    /// <param name="action"></param>
    public void ForeachExclusiveN(Action<int, int> action)
        => ForeachBase(action, r => r < n_rows, c => c < n_cols);
}
#pragma warning restore IDE1006 // Naming Styles
