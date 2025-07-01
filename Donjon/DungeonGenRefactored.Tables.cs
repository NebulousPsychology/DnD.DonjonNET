


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
    public static readonly Dictionary<string, int[,]> dungeon_layout = new(){
            {"Box", new[,]{{1,1,1},{1,0,1},{1,1,1}}},
            {"Cross", new[,]{{0,1,0},{1,1,1},{0,1,0}}},
        };

    ///<summary>get a percentage to reflect "un-curvy-ness", so straght@100, labyrinth@0</summary>
    /// <remarks><code>
    /// my $corridor_layout = {
    ///   'Labyrinth'   =>   0,
    ///   'Bent'        =>  50,
    ///   'Straight'    => 100,
    /// };
    /// </code></remarks>
    public static readonly Dictionary<string, int> corridor_layout = new()
        {
          {"Labyrinth",   0},
          {"Bent",  50},
          {"Straight", 100},
        };


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
    public static readonly Dictionary<string, dynamic> map_style = new(){
            {
                "Standard", new{
                    fill      = "000000",
                    open      = "FFFFFF",
                    open_grid = "CCCCCC",
                }
            },
        };

    #region directions

    /// <summary>1-step change in ROW position for a move to a given direction</summary>
    /// <remarks><code>
    /// my $di = { 'north' => -1, 'south' =>  1, 'west' =>  0, 'east' =>  0 };
    /// </code></remarks>
    public static readonly Dictionary<Cardinal, int> di = new() { { Cardinal.north, -1 }, { Cardinal.south, 1 }, { Cardinal.west, 0 }, { Cardinal.east, 0 } };

    /// <summary>1-step change in COLUMN position for a move to a given direction</summary>
    /// <remarks><code>
    /// my $dj = { 'north' =>  0, 'south' =>  0, 'west' => -1, 'east' =>  1 };
    /// </code></remarks>
    public static readonly Dictionary<Cardinal, int> dj = new() { { Cardinal.north, 0 }, { Cardinal.south, 0 }, { Cardinal.west, -1 }, { Cardinal.east, 1 } };

    /// <summary>
    /// keys of <see cref="dj"/>, sorted: e,n,s,w
    /// </summary>
    /// <remarks><code>
    /// my @dj_dirs = sort keys %{ $dj };
    /// </code></remarks>
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
    public static readonly Dictionary<Cardinal, Cardinal> opposite = new(){
            {Cardinal.north,Cardinal.south},
            {Cardinal.south,Cardinal.north},
            {Cardinal.west,Cardinal.east},
            {Cardinal.east,Cardinal.west},
        };

    public static readonly Dictionary<Cardinal, (int i, int j, Cardinal opposite)> directions_allinone = new() {
            { Cardinal.north, (i: -1, j: 0, opposite: Cardinal.south) },
            { Cardinal.south, (i: 1, j: 0, opposite: Cardinal.north) },
            { Cardinal.west, (i: 0, j: -1, opposite: Cardinal.east) },
            { Cardinal.east, (i: 0, j: 1, opposite: Cardinal.west) },
        };

    #endregion directions

    #region stairs
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
                    {"walled",[ (1,-1), (0,-1), (-1,-1), (-1,0), (-1,1), (0,1), (1,1), ] },
                    {"corridor",[(0,0),(1,0), (2,0)]},
                    {"stair",[(0,0)]},
                    {"next",[(1,0)]},
                }
            },
            ///   'south' => {
            ///     'walled'    => [[-1,-1],[0,-1],[1,-1],[1,0],[1,1],[0,1],[-1,1]],
            ///     'corridor'  => [[0,0],[-1,0],[-2,0]],
            ///     'stair'     => [0,0],
            ///     'next'      => [-1,0],
            ///   },
            {Cardinal.south,new(){
                ["walled"]    = [(-1,-1),(0,-1),(1,-1),(1,0),(1,1),(0,1),(-1,1)],
                ["corridor"]  = [(0,  0),(-1,0),(-2,0)],
                ["stair"]     = [( 0, 0)],
                ["next"]      = [(-1, 0)],
            }},
            ///   'west' => {
            ///     'walled'    => [[-1,1],[-1,0],[-1,-1],[0,-1],[1,-1],[1,0],[1,1]],
            ///     'corridor'  => [[0,0],[0,1],[0,2]],
            ///     'stair'     => [0,0],
            ///     'next'      => [0,1],
            ///   },
            {Cardinal.west,new(){
                ["walled"]    = [(-1,1), (-1,0),(-1,-1),(0,-1),(1,-1),(1,0),(1,1)],
                ["corridor"]  = [(0, 0),  (0,1),( 0, 2)],
                ["stair"]     = [(0, 0)],
                ["next"]      = [(0, 1)],
            }},
            ///   'east' => {
            ///     'walled'    => [[-1,-1],[-1,0],[-1,1],[0,1],[1,1],[1,0],[1,-1]],
            ///     'corridor'  => [[0,0],[0,-1],[0,-2]],
            ///     'stair'     => [0,0],
            ///     'next'      => [0,-1],
            ///   },
            /// };
            {Cardinal.east,new(){
                ["walled"]    = [(-1,-1) ,(-1,0),(-1,1),(0,1),(1,1),(1,0),(1,-1)],
                ["corridor"]  = [(0,0)   ,(0,-1),(0,-2)],
                ["stair"]     = [(0,0)],
                ["next"]      = [(0,-1)],
            }},
        };
    #endregion stairs
    #region cleaning
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