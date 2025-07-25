

using System.Diagnostics.CodeAnalysis;

using Donjon.Original;

namespace Donjon;

public partial class DungeonGenRefactored
{
    #region Magic Tables

    /// <summary>
    /// 
    /// </summary>
    /// <remarks><code>
    /// my $dungeon_layout = {
    ///   'Box'         => [[1,1,1],[1,0,1],[1,1,1]],
    ///   'Cross'       => [[0,1,0],[1,1,1],[0,1,0]],
    /// };
    /// </code></remarks>
    [Obsolete(nameof(TryGetDungeon_Layout_Mask))]
    public static readonly Dictionary<string, int[,]> dungeon_layout = new(){
            {"Box", new[,]{{1,1,1},{1,0,1},{1,1,1}}},
            {"Cross", new[,]{{0,1,0},{1,1,1},{0,1,0}}},
        };

    /// <summary>
    /// get a mask, a full square grid to filter available cells
    /// </summary>
    /// <param name="key"></param>
    /// <param name="mask"></param>
    /// <returns></returns>
    static bool TryGetDungeon_Layout_Mask(string key, [NotNullWhen(true)] out int[,]? mask)
    {
        mask = key switch
        {
            "Box" => new[,] { { 1, 1, 1 }, { 1, 0, 1 }, { 1, 1, 1 } },
            "Cross" => new[,] { { 0, 1, 0 }, { 1, 1, 1 }, { 0, 1, 0 } },
            _ => null,
        };
        return mask is not null;
    }

    ///<summary>get a percentage to reflect "un-curvy-ness", so straght@100, labyrinth@0</summary>
    /// <remarks><code>
    /// my $corridor_layout = {
    ///   'Labyrinth'   =>   0,
    ///   'Bent'        =>  50,
    ///   'Straight'    => 100,
    /// };
    /// </code></remarks>
    [Obsolete(nameof(TryGetCorridorUncurviness))]
    public static readonly Dictionary<string, int> corridor_layout = new()
        {
          {"Labyrinth",   0},
          {"Bent",  50},
          {"Straight", 100},
        };

    /// <summary>
    /// get a percentage to reflect "un-curvy-ness", so straght@100, labyrinth@0
    /// </summary>
    /// <param name="layout"></param>
    /// <param name="uncurviness">percent "un-curvy-ness"</param>
    /// <returns></returns>
    public static bool TryGetCorridorUncurviness(CorridorLayout layout, [NotNullWhen(true)] out int? uncurviness)
    {
        uncurviness = layout switch
        {
            CorridorLayout.Labyrinth => 0,
            CorridorLayout.Bent => 50,
            CorridorLayout.Straight => 100,
            _ => null,
        };
        return uncurviness is >= 0 and <= 100;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks><code>
    /// my $map_style = {
    ///   'Standard' => {
    ///     'fill'      => '000000',
    ///     'open'      => 'FFFFFF',
    ///     'open_grid' => 'CCCCCC',
    ///   },
    /// };
    /// </code></remarks>
    /// <returns></returns>
    [Obsolete("Image color codes are not relevant, unless present in exported json schema")]
    public static readonly Dictionary<string, dynamic> map_style = new()
    {
        ["Standard"] = new
        {
            fill = "000000",
            open = "FFFFFF",
            open_grid = "CCCCCC",
        },
    };

    #region directions

    /// <summary>1-step change in ROW position for a move to a given direction</summary>
    /// <remarks><code>
    /// my $di = { 'north' => -1, 'south' =>  1, 'west' =>  0, 'east' =>  0 };
    /// </code></remarks>
    [Obsolete(nameof(DeltaRow))]
    public static readonly Dictionary<Cardinal, int> di = new() { { Cardinal.north, -1 }, { Cardinal.south, 1 }, { Cardinal.west, 0 }, { Cardinal.east, 0 } };

    /// <summary>1-step change in ROW position for a move to a given direction</summary>
    [Obsolete("cast to Coord")]
    public static int DeltaRow(Cardinal direction) => direction switch
    {
        Cardinal.north => -1,
        Cardinal.south => +1,
        Cardinal.east => 0,
        Cardinal.west => 0,
        _ => throw new ArgumentException("invalid parameter"),
    };


    /// <summary>1-step change in COLUMN position for a move to a given direction</summary>
    /// <remarks><code>
    /// my $dj = { 'north' =>  0, 'south' =>  0, 'west' => -1, 'east' =>  1 };
    /// </code></remarks>
    [Obsolete(nameof(DeltaCol))]
    public static readonly Dictionary<Cardinal, int> dj = new() { { Cardinal.north, 0 }, { Cardinal.south, 0 }, { Cardinal.west, -1 }, { Cardinal.east, 1 } };

    /// <summary>1-step change in COLUMN position for a move to a given direction</summary>
    [Obsolete("cast to Coord")]
    public static int DeltaCol(Cardinal direction) => direction switch
    {
        Cardinal.north => 0,
        Cardinal.south => 0,
        Cardinal.east => +1,
        Cardinal.west => -1,
        _ => throw new ArgumentException("invalid parameter"),
    };

    /// <summary>
    /// keys of <see cref="dj"/>, sorted: e,n,s,w
    /// </summary>
    /// <remarks><code>
    /// my @dj_dirs = sort keys %{ $dj };
    /// </code></remarks>
    [Obsolete($"Enum.GetValues<{nameof(Cardinal)}>()", error: true)]
    public static readonly List<Cardinal> dj_dirs = dj.Keys.Order().ToList();


    /// <summary>
    /// 
    /// </summary>
    /// <remarks><code>
    /// my $opposite = {
    ///   'north'       => 'south',
    ///   'south'       => 'north',
    ///   'west'        => 'east',
    ///   'east'        => 'west'
    /// };
    /// </code></remarks>
    /// <returns></returns>
    [Obsolete(nameof(Opposite))]
    public static readonly Dictionary<Cardinal, Cardinal> opposite = new(){
            {Cardinal.north,Cardinal.south},
            {Cardinal.south,Cardinal.north},
            {Cardinal.west,Cardinal.east},
            {Cardinal.east,Cardinal.west},
        };
    public static Cardinal Opposite(Cardinal a) => a switch
    {
        Cardinal.east => Cardinal.west,
        Cardinal.north => Cardinal.south,
        Cardinal.west => Cardinal.east,
        Cardinal.south => Cardinal.north,
        _ => throw new ArgumentException("unrecognized"),
    };

    [Obsolete(nameof(OppositeLookup))]
    public static readonly Dictionary<Cardinal, (int i, int j, Cardinal opposite)> directions_allinone = new() {
            { Cardinal.north, (i: -1, j: 0, opposite: Cardinal.south) },
            { Cardinal.south, (i: 1, j: 0, opposite: Cardinal.north) },
            { Cardinal.west, (i: 0, j: -1, opposite: Cardinal.east) },
            { Cardinal.east, (i: 0, j: 1, opposite: Cardinal.west) },
        };

    public static void OppositeLookup(Cardinal a, out Cardinal opposite, out int r, out int c)
        => (opposite, r, c) = a switch
        {
            Cardinal.east => (Cardinal.west, 0, 1),
            Cardinal.north => (Cardinal.south, 1, 0),
            Cardinal.west => (Cardinal.east, 0, -1),
            Cardinal.south => (Cardinal.north, -1, 0),
            _ => throw new ArgumentException("unrecognized"),
        };
    /// <summary>
    /// prune operation subject. has a Next Cell and a set of coordinates to test as Walled
    /// </summary>
    public interface IPrunableSubject
    {
        /// <summary>
        /// offset to reach the next cell 
        /// </summary>
        public Coord Next { get; init; }
        public IEnumerable<Coord> Walled { get; init; }
    }
    #endregion directions

    /// <summary>
    /// Args governing Stair placement
    /// </summary>
    public readonly struct StairWallingArguments : IPrunableSubject
    {
        /// <summary>
        /// the offset to reach the next cell 
        /// </summary>
        public Coord Next { get; init; }
        public IEnumerable<Coord> Walled { get; init; }
        /// <summary>
        /// Zero, and 2 cells opposite the Direction of <see cref="Next"/>
        /// </summary>
        public IEnumerable<Coord> Corridor { get; init; }
        public static Coord Stair => Coord.Zero;
        public static StairWallingArguments Create(Coord direction, IEnumerable<Coord> walled) => new()
        {
            Next = -direction,
            Corridor = [Coord.Zero, -direction, -direction * 2],
            Walled = walled,
        };

        [Obsolete("enum requires a rotate operation, not just a skip", error: true)]
        public static StairWallingArguments Create(Coord direction, bool clockwise) => Create(
            direction: direction,
            walled: Enumerable.Where<Coord>(clockwise
                ? throw new NotImplementedException()
                : throw new NotImplementedException(),
                v => v != direction));
    }

    #region stairs
    /// <summary>
    /// 
    /// <list type="bullet">walled on all sides but the key</list>
    /// <list type="bullet">corridor zero and 2 steps opposite the key</list>
    /// <list type="bullet">stair=zero</list>
    /// <list type="bullet">next=per key</list>
    /// </summary>
    public static readonly Dictionary<Cardinal, StairWallingArguments> stair_end2 = new()
    {
        [Cardinal.north] = StairWallingArguments.Create(Coord.North, [ Coord.SouthWest, Coord.West, Coord.NorthWest,
                                                            Coord.North,// CW every direction after South except South
                                                            Coord.NorthEast, Coord.East, Coord.SouthEast, ]),
        [Cardinal.south] = StairWallingArguments.Create(Coord.South, [Coord.NorthWest,Coord.West,Coord.SouthWest,
                                                            Coord.South , //CCW every direction after North except north
                                                            Coord.SouthEast, Coord.East ,Coord.NorthEast]),
        [Cardinal.east] = StairWallingArguments.Create(Coord.East, [Coord.NorthWest ,Coord.North,Coord.NorthEast,
                                                            Coord.East,//!// CW after w but w
                                                            Coord.SouthEast,Coord.South,Coord.SouthWest]),
        [Cardinal.west] = StairWallingArguments.Create(Coord.West, [Coord.NorthEast,Coord.North,Coord.NorthWest,
                                                            Coord.West,//! // ccw after east but east
                                                            Coord.SouthWest,Coord.South,Coord.SouthEast]),
    };
    [Obsolete(nameof(stair_end2), error: true)]
    public static readonly Dictionary<Cardinal, Dictionary<string, ValueTuple<int, int>[]>> stair_end = new(){
            /// my $stair_end = {
            ///   'north' => {
            ///     'walled'    => [[1,-1],[0,-1],[-1,-1],[-1,0],[-1,1],[0,1],[1,1]],
            ///     'corridor'  => [[0,0],[1,0],[2,0]],
            ///     'stair'     => [0,0],
            ///     'next'      => [1,0],
            ///   },
            {
                Cardinal.north,
                new(){
                    {"walled",[ Coord.SouthWest, Coord.West, Coord.NorthWest,
                                Coord.North,
                                Coord.NorthEast, Coord.East, Coord.SouthEast, ] },// CW every direction after South except South
                    // {"walled",[ (1,-1), (0,-1), (-1,-1), (-1,0), (-1,1), (0,1), (1,1), ] },
                    
                    { "corridor",[Coord.Zero,Coord.South,Coord.South*2]},
                    {"stair",[(0,0)]},
                    {"next",[Coord.South]},
                }
            },
            ///   'south' => {
            ///     'walled'    => [[-1,-1],[0,-1],[1,-1],[1,0],[1,1],[0,1],[-1,1]],
            ///     'corridor'  => [[0,0],[-1,0],[-2,0]],
            ///     'stair'     => [0,0],
            ///     'next'      => [-1,0],
            ///   },
            {Cardinal.south,new(){
                // ["walled"]    = [(-1,-1),(0,-1),(1,-1),(1,0),(1,1),(0,1),(-1,1)],
                ["walled"]    = [Coord.NorthWest,Coord.West,Coord.SouthWest,
                                Coord.South ,
                                Coord.SouthEast, Coord.East ,Coord.NorthEast], //CCW every direction after North except north
                ["corridor"]  = [Coord.Zero, Coord.North,2*Coord.North],
                ["stair"]     = [Coord.Zero],
                ["next"]      = [Coord.North],
            }},
            ///   'west' => {
            ///     'walled'    => [[-1,1],[-1,0],[-1,-1],  [0,-1],  [1,-1],[1,0],[1,1]],
            ///     'corridor'  => [[0,0],[0,1],[0,2]],
            ///     'stair'     => [0,0],
            ///     'next'      => [0,1],
            ///   },
            {Cardinal.west,new(){
                ["walled"]    = [Coord.NorthEast,Coord.North,Coord.NorthWest,
                                Coord.West,//!
                                Coord.SouthWest,Coord.South,Coord.SouthEast], // ccw after east but east
                ["corridor"]  = [Coord.Zero, Coord.East,Coord.East*2 ],
                ["stair"]     = [Coord.Zero],
                ["next"]      = [Coord.East],
            }},
            ///   'east' => {
            ///     'walled'    => [[-1,-1],[-1,0],[-1,1],[0,1],[1,1],[1,0],[1,-1]],
            ///     'corridor'  => [[0,0],[0,-1],[0,-2]],
            ///     'stair'     => [0,0],
            ///     'next'      => [0,-1],
            ///   },
            /// };
            {Cardinal.east,new(){
                // ["walled"]    = [ Coord.NorthWest   ,Coord.West, Coord.NorthEast,(0,1),(1,1),(1,0),(1,-1)],
                ["walled"]    = [Coord.NorthWest ,Coord.North,Coord.NorthEast,
                                    Coord.East,//!
                                    Coord.SouthEast,Coord.South,Coord.SouthWest], // CW after w but w
                ["corridor"]  = [Coord.Zero,Coord.West,2*Coord.West],
                ["stair"]     = [(0,0)],
                ["next"]      = [ Coord.West ],
            }},
        };
    #endregion stairs
    #region cleaning
    /// <summary>
    /// Args governing whether a tunnel is Collapsible
    /// </summary>
    public readonly struct CloseWallingArguments : IPrunableSubject
    {
        public Coord Next { get; init; }
        public IEnumerable<Coord> Walled { get; init; }
        /// <summary>
        /// coord offsets (<see cref="Coord.Zero"/>) to to use when collapsing a cell to Nothing 
        /// </summary>
        public IEnumerable<Coord> Close => [Coord.Zero];
        public static CloseWallingArguments Create(Coord direction, IEnumerable<Coord> coords) => new()
        {
            Next = direction,
            Walled = coords,
        };
    }

    public static readonly Dictionary<Cardinal, CloseWallingArguments> close_end2 = new()
    {
        [Cardinal.north] = CloseWallingArguments.Create(Coord.North, [
            Coord.West, Coord.SouthWest, Coord.South, Coord.SouthEast, Coord.East
        ]),
        [Cardinal.south] = CloseWallingArguments.Create(Coord.South, [
            Coord.West, Coord.NorthWest, Coord.North, Coord.NorthEast, Coord.East
        ]),
        [Cardinal.west] = CloseWallingArguments.Create(Coord.West, [
            Coord.North,Coord.NorthEast,Coord.East,Coord.SouthEast,Coord.South
        ]),
        [Cardinal.east] = CloseWallingArguments.Create(Coord.East, [
            Coord.North, Coord.NorthWest, Coord.West, Coord.SouthWest, Coord.South
        ]),
    };
    [Obsolete(nameof(close_end2), error: true)]
    public static readonly Dictionary<Cardinal, Dictionary<string, ValueTuple<int, int>[]>> close_end = new()
    {
        /// my $close_end = {
        ///   'north' => {
        ///     'walled'    => [[0,-1],[1,-1],[1,0],[1,1],[0,1]],
        ///     'close'     => [[0,0]],
        ///     'recurse'   => [-1,0],
        ///   },
        [Cardinal.north] = new()
        {
            ["walled"] = [(0, -1), (1, -1), (1, 0), (1, 1), (0, 1)],
            ["close"] = [(0, 0)],
            ["recurse"] = [(-1, 0)],
        },
        ///   'south' => {
        ///     'walled'    => [[0,-1],[-1,-1],[-1,0],[-1,1],[0,1]],
        ///     'close'     => [[0,0]],
        ///     'recurse'   => [1,0],
        ///   },
        [Cardinal.south] = new()
        {
            ["walled"] = [(0, -1), (-1, -1), (-1, 0), (-1, 1), (0, 1)],
            ["close"] = [(0, 0)],
            ["recurse"] = [(1, 0)],
        },
        ///   'west' => {
        ///     'walled'    => [[-1,0],[-1,1],[0,1],[1,1],[1,0]],
        ///     'close'     => [[0,0]],
        ///     'recurse'   => [0,-1],
        ///   },
        [Cardinal.west] = new()
        {
            ["walled"] = [(-1, 0), (-1, 1), (0, 1), (1, 1), (1, 0)],
            ["close"] = [(0, 0)],
            ["recurse"] = [(0, -1)],
        },
        ///   'east' => {
        ///     'walled'    => [[-1,0],[-1,-1],[0,-1],[1,-1],[1,0]],
        ///     'close'     => [[0,0]],
        ///     'recurse'   => [0,1],
        ///   },
        /// };
        [Cardinal.east] = new()
        {
            ["walled"] = [(-1, 0), (-1, -1), (0, -1), (1, -1), (1, 0)],
            ["close"] = [(0, 0)],
            ["recurse"] = [(0, 1)],
        },
    };
    #endregion cleaning
    #region imaging
    /// my $color_chain = {
    ///   'door'        => 'fill',
    ///   'label'       => 'fill',
    ///   'stair'       => 'wall',
    ///   'wall'        => 'fill',
    ///   'fill'        => 'black',
    /// };
    [Obsolete("not needed unless by exported json schema")]
    public static readonly Dictionary<string, string> color_chain = new()
    {
        ["door"] = "fill",
        ["label"] = "fill",
        ["stair"] = "wall",
        ["wall"] = "fill",
        ["fill"] = "black",
    };
    #endregion imaging
    #endregion

}