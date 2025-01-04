// Adapted from https://donjon.bin.sh/code/dungeon/dungeon.pl
// https://creativecommons.org/licenses/by-nc/3.0/
using System.Collections.Immutable;
using System.Text.Json;

using Microsoft.Extensions.Logging;

namespace Donjon.Original;
#pragma warning disable IDE1006 // Naming Styles

/// <summary>
/// 
/// # Random Dungeon Generator by drow
/// # http://donjon.bin.sh/
/// #
/// # This code is provided under the
/// # Creative Commons Attribution-NonCommercial 3.0 Unported License
/// # http://creativecommons.org/licenses/by-nc/3.0/
/// </summary>
/// <typeparam name="DungeonGen"></typeparam>
public partial class DungeonGen(ILogger<DungeonGen> logger)
{
    private static readonly JsonSerializerOptions jsonLoggingOptions = new() { WriteIndented = false };


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

    #region showtime
    /// my $opts = &get_opts();
    /// my $dungeon = &create_dungeon($opts);
    ///    &image_dungeon($dungeon);
    #endregion showtime

    #region get dungeon options
    /// sub get_opts {
    ///   my $opts = {
    ///     'seed'              => time(),
    ///     'n_rows'            => 39,          # must be an odd number
    ///     'n_cols'            => 39,          # must be an odd number
    ///     'dungeon_layout'    => 'None',
    ///     'room_min'          => 3,           # minimum room size
    ///     'room_max'          => 9,           # maximum room size
    ///     'room_layout'       => 'Scattered', # Packed, Scattered
    ///     'corridor_layout'   => 'Bent',
    ///     'remove_deadends'   => 50,          # percentage
    ///     'add_stairs'        => 2,           # number of stairs
    ///     'map_style'         => 'Standard',
    ///     'cell_size'         => 18,          # pixels
    ///   };
    ///   return $opts;
    /// }
    #endregion get dungeon options

    #region create dungeon

    /// <remarks> <code>
    /// sub create_dungeon {
    ///   my ($dungeon) = @_;
    /// 
    ///   $dungeon->{'n_i'} = int($dungeon->{'n_rows'} / 2);
    ///   $dungeon->{'n_j'} = int($dungeon->{'n_cols'} / 2);
    ///   $dungeon->{'n_rows'} = $dungeon->{'n_i'} * 2;
    ///   $dungeon->{'n_cols'} = $dungeon->{'n_j'} * 2;
    ///   $dungeon->{'max_row'} = $dungeon->{'n_rows'} - 1;
    ///   $dungeon->{'max_col'} = $dungeon->{'n_cols'} - 1;
    ///   $dungeon->{'n_rooms'} = 0;
    /// 
    ///   my $max = $dungeon->{'room_max'};
    ///   my $min = $dungeon->{'room_min'};
    ///   $dungeon->{'room_base'} = int(($min + 1) / 2);
    ///   $dungeon->{'room_radix'} = int(($max - $min) / 2) + 1;
    /// 
    ///   $dungeon = &init_cells($dungeon);
    ///   $dungeon = &emplace_rooms($dungeon);
    ///   $dungeon = &open_rooms($dungeon);
    ///   $dungeon = &label_rooms($dungeon);
    ///   $dungeon = &corridors($dungeon);
    ///   $dungeon = &emplace_stairs($dungeon) if ($dungeon->{'add_stairs'});
    ///   $dungeon = &clean_dungeon($dungeon);
    /// 
    ///   return $dungeon;
    /// }
    /// </code> </remarks>
    public Dungeon Create_dungeon(Dungeon dungeon)
    {
        using (logger.BeginScope(nameof(Create_dungeon)))
        {
            dungeon = init_cells(dungeon);
            logger.LogInformation(1, "dungeonState {s}", DescribeDungeon(dungeon));

            dungeon = emplace_rooms(dungeon);
            logger.LogInformation(2, "dungeonState {s}", DescribeDungeonLite(dungeon));

            dungeon = open_rooms(dungeon);
            dungeon = label_rooms(dungeon);
            logger.LogInformation(3, "dungeonState {s}", DescribeDungeonLite(dungeon));

            dungeon = corridors(dungeon);
            logger.LogInformation(4, "dungeonState {s}", DescribeDungeonLite(dungeon));

            if (dungeon.add_stairs != 0)
            {
                dungeon = emplace_stairs(dungeon);
                logger.LogInformation(5, "dungeonState {s}", DescribeDungeonLite(dungeon));
            }

            dungeon = clean_dungeon(dungeon);
            logger.LogInformation(6, "dungeonState {s}", DescribeDungeonLite(dungeon));

            logger.LogInformation(7, "Finishing {n}", nameof(Create_dungeon));
        }
        logger.LogInformation(8, "dungeonState {s}", DescribeDungeon(dungeon));
        return dungeon;
    }
    #endregion create dungeon

    #region initialize cells

    /// <remarks><code>
    /// sub init_cells {
    ///   my ($dungeon) = @_;
    ///
    ///   my $r; for ($r = 0; $r <= $dungeon->{'n_rows'}; $r++) {
    ///     my $c; for ($c = 0; $c <= $dungeon->{'n_cols'}; $c++) {
    ///       $dungeon->{'cell'}[$r][$c] = $NOTHING;
    ///     }
    ///   }
    ///   srand($dungeon->{'seed'} + 0);
    /// 
    ///   my $mask; if ($mask = $dungeon_layout->{$dungeon->{'dungeon_layout'}}) {
    ///     $dungeon = &mask_cells($dungeon,$mask);
    ///   } elsif ($dungeon->{'dungeon_layout'} eq 'Round') {
    ///     $dungeon = &round_mask($dungeon);
    ///   }
    ///   return $dungeon;
    /// }
    /// <remarks><code>
    Dungeon init_cells(Dungeon dungeon)
    {
        using (logger.BeginScope(nameof(init_cells)))
        {
            // dungeon.cell = new Cellbits[dungeon.n_rows, dungeon.n_cols];
            for (int r = 0; r < dungeon.n_rows; r++)
            {
                for (int c = 0; c < dungeon.n_cols; c++)
                {
                    dungeon.cell[r, c] = Cellbits.NOTHING;
                }
            }
            // srand(dungeon.seed+0)
            // dungeon.random = new Random((int)dungeon.seed);
            if (dungeon_layout.TryGetValue(dungeon.dungeon_layout, out var mask))
            {
                dungeon = mask_cells(dungeon, mask);
            }
            else if (dungeon.dungeon_layout.Equals("Round"))
            {
                dungeon = round_mask(dungeon);
            }
            return dungeon;
        }
    }

    /// <remarks> <code>
    /// sub mask_cells {
    ///   my ($dungeon,$mask) = @_;
    ///   my $r_x = (scalar @{ $mask } * 1.0 / ($dungeon->{'n_rows'} + 1));
    ///   my $c_x = (scalar @{ $mask->[0] } * 1.0 / ($dungeon->{'n_cols'} + 1));
    ///   my $cell = $dungeon->{'cell'};
    /// 
    ///   my $r; for ($r = 0; $r <= $dungeon->{'n_rows'}; $r++) {
    ///     my $c; for ($c = 0; $c <= $dungeon->{'n_cols'}; $c++) {
    ///       $cell->[$r][$c] = $BLOCKED unless ($mask->[$r * $r_x][$c * $c_x]);
    ///     }
    ///   }
    ///   return $dungeon;
    /// }
    /// </code> </remarks>
    Dungeon mask_cells(Dungeon dungeon, int[,] mask)
    {
        using (logger.BeginScope(nameof(mask_cells)))
        {
            //* scale the mask coordinates up to dungeon dimensions
            //::   my $r_x = (scalar @{ $mask } * 1.0 / ($dungeon->{'n_rows'} + 1));
            var r_x = mask.GetLength(0) * 1 / (dungeon.n_rows + 1);
            //::   my $c_x = (scalar @{ $mask->[0] } * 1.0 / ($dungeon->{'n_cols'} + 1));
            var c_x = mask.GetLength(1) * 1 / (dungeon.n_cols + 1);//? should transpose getlen index?

            //::   my $r; for ($r = 0; $r <= $dungeon->{'n_rows'}; $r++) {
            for (int r = 0; r <= dungeon.n_rows; r++)
            {
                //::     my $c; for ($c = 0; $c <= $dungeon->{'n_cols'}; $c++) {
                for (int c = 0; c <= dungeon.n_cols; c++)
                {
                    //::       $cell->[$r][$c] = $BLOCKED unless ($mask->[$r * $r_x][$c * $c_x]);
                    dungeon.cell[r, c] = (mask[r * r_x, c * c_x] != 0) ? dungeon.cell[r, c] : Cellbits.BLOCKED;
                }
            }
            return dungeon;
        }
    }

    /// <remarks> <code>
    /// sub round_mask {
    ///   my ($dungeon) = @_;
    ///   my $center_r = int($dungeon->{'n_rows'} / 2);
    ///   my $center_c = int($dungeon->{'n_cols'} / 2);
    ///   my $cell = $dungeon->{'cell'};
    /// 
    ///   my $r; for ($r = 0; $r <= $dungeon->{'n_rows'}; $r++) {
    ///     my $c; for ($c = 0; $c <= $dungeon->{'n_cols'}; $c++) {
    ///       my $d = sqrt((($r - $center_r) ** 2) + (($c - $center_c) ** 2));
    ///       $cell->[$r][$c] = $BLOCKED if ($d > $center_c);
    ///     }
    ///   }
    ///   return $dungeon;
    /// }
    /// </code> </remarks>
    Dungeon round_mask(Dungeon dungeon)
    {
        using (logger.BeginScope(nameof(round_mask)))
        {
            //   my $center_r = int($dungeon->{'n_rows'} / 2);
            //   my $center_c = int($dungeon->{'n_cols'} / 2);
            var centerOfMap = (r: dungeon.n_rows / 2, c: dungeon.n_cols / 2);
            //   my $cell = $dungeon->{'cell'};

            //   my $r; for ($r = 0; $r <= $dungeon->{'n_rows'}; $r++) {
            //     my $c; for ($c = 0; $c <= $dungeon->{'n_cols'}; $c++) {
            for (int r = 0; r <= dungeon.n_rows; r++)
            {
                for (int c = 0; c <= dungeon.n_cols; c++)
                {
                    //       my $d = sqrt((($r - $center_r) ** 2) + (($c - $center_c) ** 2));
                    var radius = Math.Sqrt(Math.Pow(r - centerOfMap.r, 2) + Math.Pow(c - centerOfMap.c, 2));
                    //       $cell->[$r][$c] = $BLOCKED if ($d > $center_c);
                    dungeon.cell[r, c] = (radius > centerOfMap.c) ? Cellbits.BLOCKED : dungeon.cell[r, c];
                }
            }
            return dungeon;
        }
    }
    #endregion initialize cells

    #region place rooms
    /// <summary>place rooms, according to the chosen layout strategy</summary>
    /// <remarks><code>
    /// # emplace rooms
    /// 
    /// sub emplace_rooms {
    ///   my ($dungeon) = @_;
    /// 
    ///   if ($dungeon->{'room_layout'} eq 'Packed') {
    ///     $dungeon = &pack_rooms($dungeon);
    ///   } else {
    ///     $dungeon = &scatter_rooms($dungeon);
    ///   }
    ///   return $dungeon;
    /// }
    /// </code>
    /// </remarks>
    Dungeon emplace_rooms(Dungeon dungeon)
    {
        using (logger.BeginScope(nameof(emplace_room)))
        {
            dungeon = dungeon.room_layout switch
            {
                RoomLayout.Packed => pack_rooms(dungeon),
                RoomLayout.Scattered => scatter_rooms(dungeon),
                _ => throw new Exception($"Unrecognized case {dungeon.room_layout}"),
            };
            logger.LogInformation("Emplaced {n} rooms", dungeon.room.Count);
            return dungeon;
        }
    }


    /// <summary>room placement strategy, using "Pack": step2 across the dungeon, if a cell is not already a room</summary>
    /// <remarks><code>
    /// sub pack_rooms {
    ///   my ($dungeon) = @_;
    ///   my $cell = $dungeon->{'cell'};
    /// 
    ///   my $i; for ($i = 0; $i < $dungeon->{'n_i'}; $i++) {
    ///       my $r = ($i * 2) + 1;
    ///     my $j; for ($j = 0; $j < $dungeon->{'n_j'}; $j++) {
    ///       my $c = ($j * 2) + 1;
    /// 
    ///       next if ($cell->[$r][$c] & $ROOM);
    ///       next if (($i == 0 || $j == 0) && int(rand(2)));
    /// 
    ///       my $proto = { 'i' => $i, 'j' => $j };
    ///       $dungeon = &emplace_room($dungeon,$proto);
    ///     }
    ///   }
    ///   return $dungeon;
    /// }
    /// </code></remarks>
    Dungeon pack_rooms(Dungeon dungeon)
    {
        using (logger.BeginScope(nameof(pack_rooms)))
        {
            for (int i = 0; i < dungeon.n_i; i++)
            {
                var r = 2 * i + 1;
                for (int j = 0; j < dungeon.n_j; j++)
                {
                    var c = 2 * j + 1;
                    //__ across the dungeon's hemicell space, ...

                    //::       next if ($cell->[$r][$c] & $ROOM);
                    if (dungeon.cell[r, c].HasFlag(Cellbits.ROOM)) continue;

                    logger.LogInformation("pack-request: room {i} of {n}, last id={id}", i + 1, dungeon.n_rooms,
                          dungeon.last_room_id?.ToString() ?? "null");

                    //::       next if (($i == 0 || $j == 0) && int(rand(2)));
                    if ((i == 0 || j == 0) && dungeon.random.Next(2) != 0) continue;
                    //? why single out 0th row and column for cointoss ?

                    var proto = (i, j); // coordinates in hemicell space
                    dungeon = emplace_room(dungeon, proto);
                }
            }
            return dungeon;
        }
    }

    /// <summary>room placement strategy: just place the quota of rooms, with no prototups</summary>
    /// <remarks><code>
    /// sub scatter_rooms {
    ///   my ($dungeon) = @_;
    ///   my $n_rooms = &alloc_rooms($dungeon);
    /// 
    ///   my $i; for ($i = 0; $i < $n_rooms; $i++) {
    ///     $dungeon = &emplace_room($dungeon);
    ///   }
    ///   return $dungeon;
    /// }
    /// </code></remarks>
    Dungeon scatter_rooms(Dungeon dungeon)
    {
        using (logger.BeginScope(nameof(scatter_rooms)))
        {
            var n = alloc_rooms(dungeon);
            for (int i = 0; i < n; i++)
            {
                logger.LogInformation("scatter-request: room {i} of {n}, last id={id}", i + 1, n,
                      dungeon.last_room_id?.ToString() ?? "null");
                dungeon = emplace_room(dungeon, prototup: null);
            }
            return dungeon;
        }
    }


    /// <summary> allocate number of rooms based on the ratio of dungeon area:room area   (h*w)/(roommax^2) </summary>
    /// <remarks><code>
    /// sub alloc_rooms {
    ///   my ($dungeon) = @_;
    ///   my $dungeon_area = $dungeon->{'n_cols'} * $dungeon->{'n_rows'};
    ///   my $room_area = $dungeon->{'room_max'} * $dungeon->{'room_max'};
    ///   my $n_rooms = int($dungeon_area / $room_area);
    /// 
    ///   return $n_rooms;
    /// }
    /// </code></remarks>
    int alloc_rooms(Dungeon dungeon)
    {
        logger.LogTrace(nameof(alloc_rooms));
        int dungeon_area = dungeon.n_cols * dungeon.n_rows;
        int room_area = dungeon.room_max * dungeon.room_max;
        return dungeon_area / room_area;
    }

    /// <summary>
    /// Place a room
    /// </summary>
    /// <param name="dungeon">...</param>
    /// <param name="prototup"> i,j coordinates in hemicell space:: scatter:null, pack: hemicell;  used in set_room</param>
    /// <remarks><code>
    /// sub emplace_room {
    ///   my ($dungeon,$proto) = @_;
    ///      return $dungeon if ($dungeon->{'n_rooms'} == 999);
    ///   my ($r,$c);
    ///   my $cell = $dungeon->{'cell'};
    /// 
    ///   # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    ///   # room position and size
    /// 
    ///   $proto = &set_room($dungeon,$proto);
    /// 
    ///   # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    ///   # room boundaries
    /// 
    ///   my $r1 = ( $proto->{'i'}                       * 2) + 1;
    ///   my $c1 = ( $proto->{'j'}                       * 2) + 1;
    ///   my $r2 = (($proto->{'i'} + $proto->{'height'}) * 2) - 1;
    ///   my $c2 = (($proto->{'j'} + $proto->{'width'} ) * 2) - 1;
    /// 
    ///   return $dungeon if ($r1 < 1 || $r2 > $dungeon->{'max_row'});
    ///   return $dungeon if ($c1 < 1 || $c2 > $dungeon->{'max_col'});
    /// 
    ///   # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    ///   # check for collisions with existing rooms
    /// 
    ///   my $hit = &sound_room($dungeon,$r1,$c1,$r2,$c2);
    ///      return $dungeon if ($hit->{'blocked'});
    ///   my @hit_list = keys %{ $hit };
    ///   my $n_hits = scalar @hit_list;
    ///   my $room_id;
    /// 
    ///   if ($n_hits == 0) {
    ///     $room_id = $dungeon->{'n_rooms'} + 1;
    ///     $dungeon->{'n_rooms'} = $room_id;
    ///   } else {
    ///     return $dungeon;
    ///   }
    ///   $dungeon->{'last_room_id'} = $room_id;
    /// 
    ///   # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    ///   # emplace room
    /// 
    ///   for ($r = $r1; $r <= $r2; $r++) {
    ///     for ($c = $c1; $c <= $c2; $c++) {
    ///       if ($cell->[$r][$c] & $ENTRANCE) {
    ///         $cell->[$r][$c] &= ~ $ESPACE;
    ///       } elsif ($cell->[$r][$c] & $PERIMETER) {
    ///         $cell->[$r][$c] &= ~ $PERIMETER;
    ///       }
    ///       $cell->[$r][$c] |= $ROOM | ($room_id << 6);
    ///     }
    ///   }
    ///   my $height = (($r2 - $r1) + 1) * 10;
    ///   my $width = (($c2 - $c1) + 1) * 10;
    /// 
    ///   my $room_data = {
    ///     'id' => $room_id, 'row' => $r1, 'col' => $c1,
    ///     'north' => $r1, 'south' => $r2, 'west' => $c1, 'east' => $c2,
    ///     'height' => $height, 'width' => $width, 'area' => ($height * $width)
    ///   };
    ///   $dungeon->{'room'}[$room_id] = $room_data;
    /// 
    ///   # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    ///   # block corridors from room boundary
    ///   # check for door openings from adjacent rooms
    /// 
    ///   for ($r = $r1 - 1; $r <= $r2 + 1; $r++) {
    ///     unless ($cell->[$r][$c1 - 1] & ($ROOM | $ENTRANCE)) {
    ///       $cell->[$r][$c1 - 1] |= $PERIMETER;
    ///     }
    ///     unless ($cell->[$r][$c2 + 1] & ($ROOM | $ENTRANCE)) {
    ///       $cell->[$r][$c2 + 1] |= $PERIMETER;
    ///     }
    ///   }
    ///   for ($c = $c1 - 1; $c <= $c2 + 1; $c++) {
    ///     unless ($cell->[$r1 - 1][$c] & ($ROOM | $ENTRANCE)) {
    ///       $cell->[$r1 - 1][$c] |= $PERIMETER;
    ///     }
    ///     unless ($cell->[$r2 + 1][$c] & ($ROOM | $ENTRANCE)) {
    ///       $cell->[$r2 + 1][$c] |= $PERIMETER;
    ///     }
    ///   }
    /// 
    ///   # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// 
    ///   return $dungeon;
    /// }
    /// </code></remarks>
    Dungeon emplace_room(Dungeon dungeon, (int i, int j)? prototup)
    {
        using (logger.BeginScope($"{nameof(emplace_room)} {prototup?.ToString() ?? "(randomized)"}"))
        {
            if (dungeon.n_rooms == 999) return dungeon;

            // # room position and size
            var proto = set_room(dungeon, prototup);

            //   # room boundaries

            // get the room realspace extents from the hemispace rectangle
            //::   my $r1 = ( $proto->{'i'}                       * 2) + 1;
            //::   my $c1 = ( $proto->{'j'}                       * 2) + 1;
            //::   my $r2 = (($proto->{'i'} + $proto->{'height'}) * 2) - 1;
            //::   my $c2 = (($proto->{'j'} + $proto->{'width'} ) * 2) - 1;
            // TODO: set_room could carry the responsibility for converting back to realspace?
            var r1 = (proto["i"] * 2) + 1;
            var c1 = (proto["j"] * 2) + 1;
            var r2 = ((proto["i"] + proto["height"]) * 2) - 1;
            var c2 = ((proto["j"] + proto["width"]) * 2) - 1;

            int proposed_room_id = dungeon.n_rooms + 1; //! room_id moved from below
            logger.LogDebug("requesting room [{id}]: {ext}", proposed_room_id, string.Join(",", (object[])[(r1, c1), (r2, c2)]));

            // if any corner breaks the outermost border, eject
            if (r1 < 1 || r2 > dungeon.max_row) return dungeon;
            if (c1 < 1 || c2 > dungeon.max_col) return dungeon;

            //   # check for collisions with existing rooms
            using (logger.BeginScope("mini:collisiontest"))
            {
                //::   my $hit = &sound_room($dungeon,$r1,$c1,$r2,$c2);
                Dictionary<string, int> hit = sound_room(dungeon, r1, c1, r2, c2);
                //      return $dungeon if ($hit->{'blocked'});
                if (hit.ContainsKey("blocked"))
                {
                    logger.LogTrace("sounding resulted in block");
                    return dungeon;
                }
                //::   my @hit_list = keys %{ $hit }; 
                //::   my $n_hits = scalar @hit_list;
                int n_hits = hit.Count;
                //::   my $room_id;
                //! room_id moved outside the logging scope

                if (n_hits == 0)
                {
                    dungeon.n_rooms = proposed_room_id;
                    logger.LogDebug("Room {r} @[{extents}]: approved because no hits", proposed_room_id, string.Join(",", (object[])[(r1, c1), (r2, c2)]));
                }
                else
                {
                    logger.LogInformation("Room {r} @[{extents}]: rejected because hits={h} > 0 ", proposed_room_id, string.Join(",", (object[])[(r1, c1), (r2, c2)]), n_hits);
                    return dungeon;
                }
                dungeon.last_room_id = proposed_room_id; // The room id is issued
            }

            // # emplace room
            using (logger.BeginScope("mini:emplaceroom"))
            {
                for (int r = r1; r <= r2; r++)
                {
                    for (int c = c1; c <= c2; c++)
                    {
                        // remove Entrance marker
                        if (dungeon.cell[r, c].HasFlag(Cellbits.ENTRANCE)) //::       if ($cell->[$r][$c] & $ENTRANCE) {
                        {
                            dungeon.cell[r, c] &= ~Cellbits.ESPACE;
                        }
                        else if (dungeon.cell[r, c].HasFlag(Cellbits.PERIMETER)) //::       } elsif ($cell->[$r][$c] & $PERIMETER) {
                        {
                            // remove Perimiter marker
                            dungeon.cell[r, c] &= ~Cellbits.PERIMETER;
                        }

                        // Add room marker, plus the room Id
                        //::       $cell->[$r][$c] |= $ROOM | ($room_id << 6);
                        dungeon.cell[r, c] |= Cellbits.ROOM | (Cellbits)(proposed_room_id << 6);
                    }
                }

                int cellsize = 1; //! is an alteration vs perl: original specifies 10
                IDungeonRoom room_data = new DungeonRoomStruct
                {
                    id = proposed_room_id,
                    row = r1,
                    col = c1,
                    north = r1,
                    south = r2,
                    west = c1,
                    east = c2,
                    height = ((r2 - r1) + 1) * cellsize,
                    width = ((c2 - c1) + 1) * cellsize,
                    door = [],
                };
                dungeon.room[proposed_room_id] = room_data;
                logger.LogInformation(message: "AddRoom: {r}", JsonSerializer.Serialize(room_data, jsonLoggingOptions));
            }

            //   # block corridors from room boundary
            //   # check for door openings from adjacent rooms
            using (logger.BeginScope("mini:block room bound"))
            {
                for (int r = r1 - 1; r <= r2 + 1; r++)
                {
                    foreach (int horizedgeindex in (IEnumerable<int>)[c1 - 1, c2 + 1])
                    {
                        //     unless ($cell->[$r][$c1 - 1] & ($ROOM | $ENTRANCE)) {
                        if (!dungeon.cell[r, horizedgeindex].HasAnyFlag(Cellbits.ROOM | Cellbits.ENTRANCE)) //! or-entrance is NOT an alteration vs perl!
                        {
                            //       $cell->[$r][$cX - 1] |= $PERIMETER;
                            dungeon.cell[r, horizedgeindex] |= Cellbits.PERIMETER;
                        }
                    }
                }

                for (int c = c1 - 1; c <= c2 + 1; c++)
                {
                    foreach (int vertedgeindex in (IEnumerable<int>)[r1 - 1, r2 + 1])
                    {
                        //     unless ($cell->[$r][$c1 - 1] & ($ROOM | $ENTRANCE)) {
                        if (!dungeon.cell[vertedgeindex, c].HasAnyFlag(Cellbits.ROOM | Cellbits.ENTRANCE))
                        {
                            //       $cell->[$r][$cX - 1] |= $PERIMETER;
                            dungeon.cell[vertedgeindex, c] |= Cellbits.PERIMETER;
                        }
                    }
                }
            }// /scope
            return dungeon;
        }
    }


    /// <remarks><code>
    /// sub set_room {
    ///   my ($dungeon,$proto) = @_;
    ///   my $base = $dungeon->{'room_base'};
    ///   my $radix = $dungeon->{'room_radix'};
    /// 
    ///   unless (defined $proto->{'height'}) {
    ///     if (defined $proto->{'i'}) {
    ///       my $a = $dungeon->{'n_i'} - $base - $proto->{'i'};
    ///          $a = 0 if ($a < 0);
    ///       my $r = ($a < $radix) ? $a : $radix;
    /// 
    ///       $proto->{'height'} = int(rand($r)) + $base;
    ///     } else {
    ///       $proto->{'height'} = int(rand($radix)) + $base;
    ///     }
    ///   }
    ///   unless (defined $proto->{'width'}) {
    ///     if (defined $proto->{'j'}) {
    ///       my $a = $dungeon->{'n_j'} - $base - $proto->{'j'};
    ///          $a = 0 if ($a < 0);
    ///       my $r = ($a < $radix) ? $a : $radix;
    /// 
    ///       $proto->{'width'} = int(rand($r)) + $base;
    ///     } else {
    ///       $proto->{'width'} = int(rand($radix)) + $base;
    ///     }
    ///   }
    ///   unless (defined $proto->{'i'}) {
    ///     $proto->{'i'} = int(rand($dungeon->{'n_i'} - $proto->{'height'}));
    ///   }
    ///   unless (defined $proto->{'j'}) {
    ///     $proto->{'j'} = int(rand($dungeon->{'n_j'} - $proto->{'width'}));
    ///   }
    ///   return $proto;
    /// }
    /// </code></remarks>
    /// <summary>room position and size</summary>
    /// <returns>
    ///     a dictionary [i,j,height,width] representing a rectangle in hemispace:
    ///     where i&j are either the original hemispace coords OR a random hemispace coord that can fit the room
    ///     FIXME: uncertainty remains on whether this directly maps to Rectangle types
    /// </returns>
    IDictionary<string, int> set_room(Dungeon dungeon, (int i, int j)? prototuple)
    {
        using (logger.BeginScope(nameof(set_room)))
        {
            var roomBase = dungeon.room_base;//? even, if room_minmax are obligate odd
            var roomRadix = dungeon.room_radix; //? odd, if room_minmax are obligate odd
            int createRadix(int hemicelladdress, int hemicellmax)
                => Math.Min(roomRadix, Math.Max(0, hemicellmax - roomBase - hemicelladdress));
#if Original
            Dictionary<string, int> proto = prototuple is null ? [] : new()
            {
                ["i"] = prototuple.Value.i,
                ["j"] = prototuple.Value.j,
            };

            //::   unless (defined $proto->{'height'}) {
            if (!proto.ContainsKey("height"))
            {
                var rdx = prototuple.HasValue ? fromprototup(prototuple.Value.i, dungeon.n_i) : roomRadix;
                proto.Add("height", dungeon.random.Next(rdx) + roomBase);
            }
            //::   unless (defined $proto->{'width'}) {
            if (!proto.ContainsKey("width"))
            {
                var radx = prototuple.HasValue ? fromprototup(prototuple.Value.j, dungeon.n_j) : roomRadix;
                proto.Add("width", dungeon.random.Next(radx) + roomBase);
            }
            //::   unless (defined $proto->{'i'}) {
            //::     $proto->{'i'} = int(rand($dungeon->{'n_i'} - $proto->{'height'}));
            //::   }
            _ = proto.TryAdd("i", dungeon.random.Next(dungeon.n_i - proto["height"]));
            //::   unless (defined $proto->{'j'}) {
            //::     $proto->{'j'} = int(rand($dungeon->{'n_j'} - $proto->{'width'}));
            //::   }
            _ = proto.TryAdd("j", dungeon.random.Next(dungeon.n_j - proto["width"]));
            return proto;
#else
            Dictionary<string, int> proto = prototuple is null ? new()
            {
                ["height"] = dungeon.random.Next(roomRadix) + roomBase,
                ["width"] = dungeon.random.Next(roomRadix) + roomBase,
            } : new()
            {
                ["i"] = prototuple.Value.i,
                ["j"] = prototuple.Value.j,
                ["height"] = dungeon.random.Next(createRadix(prototuple.Value.i, dungeon.n_i)) + roomBase,
                ["width"] = dungeon.random.Next(createRadix(prototuple.Value.j, dungeon.n_j)) + roomBase,
            };

            //__ find a hemispace coordinate that can fit a room of the chosen size
            _ = proto.TryAdd("i", dungeon.random.Next(dungeon.n_i - proto["height"]));
            _ = proto.TryAdd("j", dungeon.random.Next(dungeon.n_j - proto["width"]));

            return proto;
#endif
        }
    }

    /// <remarks><code>
    /// sub sound_room {
    ///   my ($dungeon,$r1,$c1,$r2,$c2) = @_;
    ///   my $cell = $dungeon->{'cell'};
    ///   my $hit;
    /// 
    ///   my $r; for ($r = $r1; $r <= $r2; $r++) {
    ///     my $c; for ($c = $c1; $c <= $c2; $c++) {
    ///       if ($cell->[$r][$c] & $BLOCKED) {
    ///         return { 'blocked' => 1 };
    ///       }
    ///       if ($cell->[$r][$c] & $ROOM) {
    ///         my $id = ($cell->[$r][$c] & $ROOM_ID) >> 6;
    ///         $hit->{$id} += 1;
    ///       }
    ///     }
    ///   }
    ///   return $hit;
    /// }
    /// </code></remarks>
    Dictionary<string, int> sound_room(Dungeon dungeon, int r1, int c1, int r2, int c2)
    {
        using (logger.BeginScope(nameof(sound_room)))
        {
            Dictionary<string, int> hit = [];

            for (int r = r1; r <= r2; r++)
            {
                for (int c = c1; c <= c2; c++)
                {
                    //       if ($cell->[$r][$c] & $BLOCKED) {
                    if (dungeon.cell[r, c].HasFlag(Cellbits.BLOCKED))
                    {
                        return new Dictionary<string, int> { ["blocked"] = 1 };
                    }
                    if (dungeon.cell[r, c].TryGetRoomId(out int roomid))
                    {
                        if (hit.TryGetValue(roomid.ToString(), out int prevhitcount))
                        {
                            hit[roomid.ToString()] = prevhitcount + 1;
                        }
                        else
                        {
                            hit.Add(roomid.ToString(), 1);
                        }
                    }
                }
            }
            logger.LogDebug("sounding row({a}..{b})/col({c}..{d}): {hits}", r1, r2, c1, c2,
                string.Join(",", hit.Select(kvp => $"'{kvp.Key}'={kvp.Value}")));
            return hit;
        }
    }
    #endregion place rooms

    #region doors/corridors

    ///<summary> emplace openings for doors and corridors</summary>
    /// <remarks><code>
    /// sub open_rooms {
    ///   my ($dungeon) = @_;
    /// 
    ///   my $id; for ($id = 1; $id <= $dungeon->{'n_rooms'}; $id++) {
    ///     $dungeon = &open_room($dungeon,$dungeon->{'room'}[$id]);
    ///   }
    ///   delete($dungeon->{'connect'});
    ///   return $dungeon;
    /// }
    /// </code></remarks>
    /// <param name="dungeon"></param>
    /// <returns></returns>
    Dungeon open_rooms(Dungeon dungeon)
    {
        using (logger.BeginScope(nameof(open_rooms)))
        {
            logger.LogDebug("Open rooms, batch of {n}", dungeon.room.Count);
            foreach (var id in dungeon.room.Keys) // for (int id = 0; id < dungeon.n_rooms; id++) // [1..n_rooms] == [0..nrooms)
            {
                try
                {
                    dungeon = open_room(dungeon, dungeon.room[id]);
                }
                catch (KeyNotFoundException knf)
                {
                    logger.LogError(knf, "KNF: failed to fid {id} in {range}: [{keys}]", id, dungeon.n_rooms, string.Join(",", dungeon.room.Keys));
                    throw;
                }
            }
            dungeon.connect?.Clear(); //   delete($dungeon->{'connect'});

            return dungeon;
        }
    }

    ///<summary>emplace openings for doors and corridors</summary>
    /// <remarks><code>
    /// sub open_room {
    ///   my ($dungeon,$room) = @_;
    ///   my @list = &door_sills($dungeon,$room);
    ///      return $dungeon unless (@list);
    ///   my $n_opens = &alloc_opens($dungeon,$room);
    ///   my $cell = $dungeon->{'cell'};
    /// 
    ///   my $i; for ($i = 0; $i < $n_opens; $i++) {
    ///     my $sill = splice(@list,int(rand(@list)),1);
    ///        last unless ($sill);
    ///     my $door_r = $sill->{'door_r'};
    ///     my $door_c = $sill->{'door_c'};
    ///     my $door_cell = $cell->[$door_r][$door_c];
    ///        redo if ($door_cell & $DOORSPACE);
    /// 
    ///     my $out_id; if ($out_id = $sill->{'out_id'}) {
    ///       my $connect = join(',',(sort($room->{'id'},$out_id)));
    ///       redo if ($dungeon->{'connect'}{$connect}++);
    ///     }
    ///     my $open_r = $sill->{'sill_r'};
    ///     my $open_c = $sill->{'sill_c'};
    ///     my $open_dir = $sill->{'dir'};
    /// 
    ///     # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    ///     # open door
    /// 
    ///     my $x; for ($x = 0; $x < 3; $x++) {
    ///       my $r = $open_r + ($di->{$open_dir} * $x);
    ///       my $c = $open_c + ($dj->{$open_dir} * $x);
    /// 
    ///       $cell->[$r][$c] &= ~ $PERIMETER;
    ///       $cell->[$r][$c] |= $ENTRANCE;
    ///     }
    ///     my $door_type = &door_type();
    ///     my $door = { 'row' => $door_r, 'col' => $door_c };
    /// 
    ///     if ($door_type == $ARCH) {
    ///       $cell->[$door_r][$door_c] |= $ARCH;
    ///       $door->{'key'} = 'arch'; $door->{'type'} = 'Archway';
    ///     } elsif ($door_type == $DOOR) {
    ///       $cell->[$door_r][$door_c] |= $DOOR;
    ///       $cell->[$door_r][$door_c] |= (ord('o') << 24);
    ///       $door->{'key'} = 'open'; $door->{'type'} = 'Unlocked Door';
    ///     } elsif ($door_type == $LOCKED) {
    ///       $cell->[$door_r][$door_c] |= $LOCKED;
    ///       $cell->[$door_r][$door_c] |= (ord('x') << 24);
    ///       $door->{'key'} = 'lock'; $door->{'type'} = 'Locked Door';
    ///     } elsif ($door_type == $TRAPPED) {
    ///       $cell->[$door_r][$door_c] |= $TRAPPED;
    ///       $cell->[$door_r][$door_c] |= (ord('t') << 24);
    ///       $door->{'key'} = 'trap'; $door->{'type'} = 'Trapped Door';
    ///     } elsif ($door_type == $SECRET) {
    ///       $cell->[$door_r][$door_c] |= $SECRET;
    ///       $cell->[$door_r][$door_c] |= (ord('s') << 24);
    ///       $door->{'key'} = 'secret'; $door->{'type'} = 'Secret Door';
    ///     } elsif ($door_type == $PORTC) {
    ///       $cell->[$door_r][$door_c] |= $PORTC;
    ///       $cell->[$door_r][$door_c] |= (ord('#') << 24);
    ///       $door->{'key'} = 'portc'; $door->{'type'} = 'Portcullis';
    ///     }
    ///     $door->{'out_id'} = $out_id if ($out_id);
    ///     push(@{ $room->{'door'}{$open_dir} },$door) if ($door);
    ///   }
    ///   return $dungeon;
    /// }
    /// </code></remarks>
    /// <param name="dungeon"></param>
    /// <param name="room"></param>
    /// <returns></returns>
    Dungeon open_room(Dungeon dungeon, IDungeonRoom room)
    {
        using (logger.BeginScope(nameof(open_room)))
        {
            logger.LogInformation("opening room {room}", JsonSerializer.Serialize(room, jsonLoggingOptions));
            IEnumerable<Sill> list = door_sills(dungeon, room);
            if (list is null) return dungeon;
            int n_opens = alloc_opens(dungeon, room);

            for (int i = 0; i < n_opens; i++)
            {
                //     my $sill = splice(@list,int(rand(@list)),1);
                //        last unless ($sill);
                if (!list.Any()) break;
                Sill sill = list.ElementAt(dungeon.random.Next(list.Count()));

                int door_r = sill.door_r;
                int door_c = sill.door_c;
                var door_cell = dungeon.cell[door_r, door_c];
                //        redo if ($door_cell & $DOORSPACE);
                if ((door_cell & Cellbits.DOORSPACE) != 0) { i = -1; continue; }

                if (sill.out_id.HasValue)
                {
                    //? constructs the connection string and checks if it has been used before
                    //       my $connect = join(',',(sort($room->{'id'},$out_id)));
                    string connect = string.Join(",", Enumerable.Order([room.id, sill.out_id.Value]));
                    //       redo if ($dungeon->{'connect'}{$connect}++);
                    if (dungeon.connect?.TryGetValue(connect, out int connectionCount) ?? false)
                    {
                        if (connectionCount > room.Perimeter)
                        {
                            logger.LogDebug("count ({v}) exceeds room perimiter ({p}) for connection {conn}",
                                connectionCount, room.Perimeter, connect);
                            continue;
                        }
                        dungeon.connect![connect] = connectionCount + 1;
                        //! `redo` is a perl notion that C# cannot replicate: If the condition inside the if statement is true, 
                        //! the loop restarts from the beginning, AFTER the increment is applied
                        i = -1; continue;
                    }
                    else if (dungeon.connect?.TryAdd(connect, 1) ?? false)
                    {
                        //! adds, but does not restart 
                    }
                }
                var open_r = sill.sill_r;
                var open_c = sill.sill_c;
                Cardinal open_dir = sill.dir;

                /// # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
                //     # open door
                using (logger.BeginScope("OpenDoor"))
                {
                    for (int x = 0; x < 3; x++)
                    {
                        int r = open_r + (di[open_dir] * x);
                        int c = open_c + (dj[open_dir] * x);

                        dungeon.cell[r, c] &= ~Cellbits.PERIMETER;
                        dungeon.cell[r, c] |= Cellbits.ENTRANCE;
                    }
                    Cellbits door_type = getdoor_type(dungeon.random);

                    (char? sign, string key, string type)? doorinfo = door_type switch
                    {
                        Cellbits.DOOR_ARCH => (sign: null, key: "arch", type: "Archway"),
                        Cellbits.DOOR_SIMPLE => (sign: 'o', key: "open", type: "Unlocked Door"),
                        Cellbits.DOOR_LOCKED => (sign: 'x', key: "lock", type: "Locked Door"),
                        Cellbits.DOOR_TRAPPED => (sign: 't', key: "trap", type: "Trapped Door"),
                        Cellbits.DOOR_SECRET => (sign: 's', key: "secret", type: "Secret Door"),
                        Cellbits.DOOR_PORTC => (sign: '#', key: "portc", type: "Portcullis"),
                        _ => throw new InvalidOperationException($"SWW: unrecognized door type {door_type}"),
                    };
                    if (doorinfo.HasValue)
                    {
                        logger.LogTrace("working on door {d} at ({r},{c})", doorinfo, door_r, door_c);
                        dungeon.cell[door_r, door_c] |= door_type;
                        dungeon.cell[door_r, door_c].SetLabel(doorinfo?.sign ?? (char)0);
                        var door = new DoorData
                        {
                            row = door_r,
                            col = door_c,
                            open_dir = open_dir,
                            key = doorinfo!.Value.key,
                            type = doorinfo!.Value.type,
                            out_id = sill.out_id
                        };

                        //     push(@{ $room->{'door'}{$open_dir} },$door) if ($door); 
                        if (!room.door.TryAdd(open_dir, [door])) room.door[open_dir].Add(door);
                        logger.LogInformation("Add door {d} at ({r},{c})", doorinfo, door_r, door_c);
                    }
                    else
                    {
                        logger.LogTrace("rejected door at ({r},{c})", door_r, door_c);
                    }
                }// /scope Opendoor
            }
            logger.LogInformation("Opened {actual} doors for {expected} attempts", room.door.Sum(directionkvp => directionkvp.Value.Count), n_opens);
            return dungeon;
        }
    }

    ///<summary> allocate number of opens</summary>
    /// <remarks><code>
    /// sub alloc_opens {
    ///   my ($dungeon,$room) = @_;
    ///   my $room_h = (($room->{'south'} - $room->{'north'}) / 2) + 1;
    ///   my $room_w = (($room->{'east'} - $room->{'west'}) / 2) + 1;
    ///   my $flumph = int(sqrt($room_w * $room_h));
    ///   my $n_opens = $flumph + int(rand($flumph));
    /// 
    ///   return $n_opens;
    /// }
    /// </code></remarks>
    /// <returns>number of openings for the room</returns>
    int alloc_opens(Dungeon dungeon, IDungeonRoom room)
    {
        using (logger.BeginScope(nameof(alloc_opens)))
        {
            int room_h = (room.width / 2) + 1;
            int room_w = (room.height / 2) + 1;
            int flumph = (int)Math.Sqrt(room_w * room_h);
            return flumph + dungeon.random.Next(flumph);
        }
    }


    ///<summary> list available sills</summary>
    /// <remarks><code>
    /// sub door_sills {
    ///   my ($dungeon,$room) = @_;
    ///   my $cell = $dungeon->{'cell'};
    ///   my @list;
    /// 
    ///   if ($room->{'north'} >= 3) {
    ///     my $c; for ($c = $room->{'west'}; $c <= $room->{'east'}; $c += 2) {
    ///       my $sill = &check_sill($cell,$room,$room->{'north'},$c,'north');
    ///       push(@list,$sill) if ($sill);
    ///     }
    ///   }
    ///   if ($room->{'south'} <= ($dungeon->{'n_rows'} - 3)) {
    ///     my $c; for ($c = $room->{'west'}; $c <= $room->{'east'}; $c += 2) {
    ///       my $sill = &check_sill($cell,$room,$room->{'south'},$c,'south');
    ///       push(@list,$sill) if ($sill);
    ///     }
    ///   }
    ///   if ($room->{'west'} >= 3) {
    ///     my $r; for ($r = $room->{'north'}; $r <= $room->{'south'}; $r += 2) {
    ///       my $sill = &check_sill($cell,$room,$r,$room->{'west'},'west');
    ///       push(@list,$sill) if ($sill);
    ///     }
    ///   }
    ///   if ($room->{'east'} <= ($dungeon->{'n_cols'} - 3)) {
    ///     my $r; for ($r = $room->{'north'}; $r <= $room->{'south'}; $r += 2) {
    ///       my $sill = &check_sill($cell,$room,$r,$room->{'east'},'east');
    ///       push(@list,$sill) if ($sill);
    ///     }
    ///   }
    ///   return &shuffle(@list);
    /// }
    /// </code></remarks>
    /// <param name="dungeon"></param>
    /// <param name="room"></param>
    /// <returns></returns>
    IEnumerable<Sill> door_sills(Dungeon dungeon, IDungeonRoom room)
    {
        using (logger.BeginScope(nameof(door_sills)))
        {
            List<Sill> list = [];
            if (room.north >= 3) // if north is inset 3 from map's top edge, there's enough room to exit north
            {
                for (int c = room.west; c <= room.east; c += 2) // Note step by 2
                {
                    Sill? sill = check_sill(dungeon.cell, room, sill_r: room.north, sill_c: c, Cardinal.north);
                    if (sill.HasValue) list.Add(sill.Value);
                }
            }
            if (room.south <= dungeon.n_rows - 3)// if south is inset 3 from map's bottom edge, there's enough room to exit south
            {
                for (int c = room.west; c <= room.east; c += 2)
                {
                    Sill? sill = check_sill(dungeon.cell, room, sill_r: room.south, sill_c: c, Cardinal.south);
                    if (sill.HasValue) list.Add(sill.Value);
                }
            }
            if (room.west >= 3) // if west is inset 3 from map's left, there's enough room to exit west
            {
                for (int r = room.north; r <= room.south; r += 2)
                {
                    Sill? sill = check_sill(dungeon.cell, room, r, room.west, Cardinal.west);
                    if (sill.HasValue) list.Add(sill.Value);
                }
            }
            if (room.east <= dungeon.n_cols - 3) // if east is inset 3 from map's right, there's enough room to exit east
            {
                for (int r = room.north; r <= room.south; r += 2)
                {
                    Sill? sill = check_sill(dungeon.cell, room, r, room.east, Cardinal.east);
                    if (sill.HasValue) list.Add(sill.Value);
                }
            }
            var ans = list.ToArray();
            dungeon.random.Shuffle(ans);
            logger.LogInformation("Produced {n} sills for room {rid}", ans.Length, room.id);
            return ans;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks><code>
    /// sub check_sill {
    ///   my ($cell,$room,$sill_r,$sill_c,$dir) = @_;
    ///   my $door_r = $sill_r + $di->{$dir};
    ///   my $door_c = $sill_c + $dj->{$dir};
    ///   my $door_cell = $cell->[$door_r][$door_c];
    ///      return unless ($door_cell & $PERIMETER);
    ///      return if ($door_cell & $BLOCK_DOOR);
    ///   my $out_r  = $door_r + $di->{$dir};
    ///   my $out_c  = $door_c + $dj->{$dir};
    ///   my $out_cell = $cell->[$out_r][$out_c];
    ///      return if ($out_cell & $BLOCKED);
    ///
    ///   my $out_id; if ($out_cell & $ROOM) {
    ///     $out_id = ($out_cell & $ROOM_ID) >> 6;
    ///     return if ($out_id == $room->{'id'});
    ///   }
    ///   return {
    ///     'sill_r'    => $sill_r,
    ///     'sill_c'    => $sill_c,
    ///     'dir'       => $dir,
    ///     'door_r'    => $door_r,
    ///     'door_c'    => $door_c,
    ///     'out_id'    => $out_id,
    ///   };
    /// }
    /// </code></remarks>
    /// <param name="cell"></param>
    /// <param name="room"></param>
    /// <param name="sill_r"></param>
    /// <param name="sill_c"></param>
    /// <param name="dir"></param>
    /// <returns></returns>
    Sill? check_sill(Cellbits[,] cell, IDungeonRoom room, int sill_r, int sill_c, Cardinal dir)
    {
        using (logger.BeginScope(nameof(check_sill)))
        {
            (int door_r, int door_c) = (sill_r + di[dir], sill_c + dj[dir]);
            //      return unless ($door_cell & $PERIMETER);
            if (false == cell[door_r, door_c].HasFlag(Cellbits.PERIMETER))
            {
                return null; // return because of not being a perimeter
            }
            //      return if ($door_cell & $BLOCK_DOOR);
            if (cell[door_r, door_c].HasFlag(Cellbits.BLOCK_DOOR))
            {
                return null; // because of being blocked
            }

            (int out_r, int out_c) = (door_r + di[dir], door_c + dj[dir]);
            Cellbits out_cell = cell[out_r, out_c];
            if (out_cell.HasFlag(Cellbits.BLOCKED)) { return null; }

            if (out_cell.TryGetRoomId(out int out_id))
            {
                if (out_id == room.id) return null;
            }
            return new()
            {
                sill_r = sill_r,
                sill_c = sill_c,
                dir = dir,
                door_r = door_r,
                door_c = door_c,
                out_id = out_id,
            };
        }
    }

    /// <summary> random door type </summary>
    /// <remarks><code>
    /// sub door_type {
    ///   my $i = int(rand(110));
    /// 
    ///   if ($i < 15) {
    ///     return $ARCH;
    ///   } elsif ($i < 60) {
    ///     return $DOOR;
    ///   } elsif ($i < 75) {
    ///     return $LOCKED;
    ///   } elsif ($i < 90) {
    ///     return $TRAPPED;
    ///   } elsif ($i < 100) {
    ///     return $SECRET;
    ///   } else {
    ///     return $PORTC;
    ///   }
    /// }
    /// <code></remarks>
    Cellbits getdoor_type(Random r)
    {
        using (logger.BeginScope(nameof(getdoor_type)))
        {
            return r.Next(110) switch
            {
                >= 00 and < 15 => Cellbits.DOOR_ARCH,
                >= 15 and < 60 => Cellbits.DOOR_SIMPLE,
                >= 60 and < 75 => Cellbits.DOOR_LOCKED,
                >= 75 and < 90 => Cellbits.DOOR_TRAPPED,
                >= 90 and < 100 => Cellbits.DOOR_SECRET,
                _ => Cellbits.DOOR_PORTC
            };
        }
    }
    #endregion doors/corridors

    #region Room Labels
    /// <summary>
    /// 
    /// </summary>
    /// <remarks><code>
    /// sub label_rooms {
    ///   my ($dungeon) = @_;
    ///   my $cell = $dungeon->{'cell'};
    /// 
    ///   my $id; for ($id = 1; $id <= $dungeon->{'n_rooms'}; $id++) {
    ///     my $room = $dungeon->{'room'}[$id];
    ///     my $label = "$room->{'id'}";
    ///     my $len = length($label);
    ///     my $label_r = int(($room->{'north'} + $room->{'south'}) / 2);
    ///     my $label_c = int(($room->{'west'} + $room->{'east'} - $len) / 2) + 1;
    /// 
    ///     my $c; for ($c = 0; $c < $len; $c++) {
    ///       my $char = substr($label,$c,1);
    ///       $cell->[$label_r][$label_c + $c] |= (ord($char) << 24);
    ///     }
    ///   }
    ///   return $dungeon;
    /// }
    /// </code></remarks>
    /// <param name="dungeon"></param>
    /// <returns></returns>
    Dungeon label_rooms(Dungeon dungeon)
    {
        using (logger.BeginScope(nameof(label_rooms)))
        {
            foreach (IDungeonRoom room in dungeon.room.Values)
            {
                string label = room.id.ToString();

                // start writing the label from the room's middle row, and a column centered on the middle of the room
                int label_r = (room.north + room.south) / 2;
                //     my $label_c = int(($room->{'west'} + $room->{'east'} - $len) / 2) + 1;
                int label_c = ((room.west + room.east - label.Length) / 2) + 1;
                logger.LogInformation("Stamping id as label on cells of room id {id}, from {r},{c}", room.id, label_r, label_c);

                for (int c = 0; c < label.Length; c++)
                {
                    dungeon.cell[label_r, label_c + c] = dungeon.cell[label_r, label_c + c].SetLabel(label[c]);
                }
            }
            return dungeon;
        }
    }
    #endregion Room Labels

    #region Corridors

    /// <summary>
    /// # generate corridors
    /// </summary>
    /// <remarks> <code>
    /// sub corridors {
    ///   my ($dungeon) = @_;
    ///   my $cell = $dungeon->{'cell'};
    ///
    ///   my $i; for ($i = 1; $i < $dungeon->{'n_i'}; $i++) {
    ///       my $r = ($i * 2) + 1;
    ///     my $j; for ($j = 1; $j < $dungeon->{'n_j'}; $j++) {
    ///       my $c = ($j * 2) + 1;
    ///
    ///       next if ($cell->[$r][$c] & $CORRIDOR);
    ///       $dungeon = &tunnel($dungeon,$i,$j);
    ///     }
    ///   }
    ///   return $dungeon;
    /// }
    /// </code> </remarks>
    /// <param name="dungeon"></param>
    /// <returns></returns>
    Dungeon corridors(Dungeon dungeon)
    {
        using (logger.BeginScope(nameof(corridors)))
        {
            //! For Odd-numbered rows and columns, starting at 3,3.  
            //! To this end, ij-indexspace is halfgrid, scaled and offset to return to rowspace

            // from 1,1 to n_i-1,n_j-1, inclusive
            for (int i = 1; i < dungeon.n_i; i++)
            {
                int r = i * 2 + 1;
                for (int j = 1; j < dungeon.n_j; j++)
                {
                    int c = j * 2 + 1;
                    logger.LogDebug(1, "Consider tunnling from ({r},{c})", r, c);
                    if (dungeon.cell[r, c].HasAnyFlag(Cellbits.CORRIDOR)) // if we see Corridor, we already tunneled at [r,c]
                    {
                        continue;
                    }
                    logger.LogDebug(2, "About to tunnel from ({r},{c}) because it isn't CORRIDOR", r, c);
                    // ? but then we snap back into index-space?
                    dungeon = tunnel(dungeon, i, j);
                    logger.LogInformation(3, "Finished a Tunnel from {fn}! \nnew map:{map}",
                        nameof(corridors), DescribeDungeonLite(dungeon));
                }
            }
            return dungeon;
        }
    }

    /// <summary>
    ///  recursively tunnel, in odds-indexspace
    /// </summary>
    /// <remarks><code>
    /// sub tunnel {
    ///   my ($dungeon,$i,$j,$last_dir) = @_;
    ///   my @dirs = &tunnel_dirs($dungeon,$last_dir);
    /// 
    ///   my $dir; foreach $dir (@dirs) {
    ///     if (&open_tunnel($dungeon,$i,$j,$dir)) {
    ///       my $next_i = $i + $di->{$dir};
    ///       my $next_j = $j + $dj->{$dir};
    /// 
    ///       $dungeon = &tunnel($dungeon,$next_i,$next_j,$dir);
    ///     }
    ///   }
    ///   return $dungeon;
    /// }
    /// </code></remarks>
    /// <param name="dungeon"></param>
    /// <param name="i">3+2n odd index space row</param>
    /// <param name="j">3+2n odd index space col</param>
    /// <param name="lastdir"></param>
    /// <returns></returns>
    Dungeon tunnel(Dungeon dungeon, int i, int j, Cardinal? lastdir = null)
    {
        // using (logger.BeginScope(nameof(tunnel)))
        // {
        IEnumerable<Cardinal> dirs = tunnel_dirs(dungeon, lastdir);
        logger.LogTrace("Tunneling [{dirs}] from indexspace({i},{j})=rowspace({r},{c})",
            string.Join(",", dirs), i, j, i * 2 + 1, j * 2 + 1);
        foreach (var dir in dirs)
        {
            if (open_tunnel(dungeon, i, j, dir))
            {
                (int next_i, int next_j) = (i + di[dir], j + dj[dir]);
                dungeon = tunnel(dungeon, next_i, next_j, dir);
            }
        }
        return dungeon;
        // }
    }

    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    /// <summary>
    /// tunnel directions priority list, accounting for tendency to continue in the last direction (<see cref="corridor_layout"/>)
    /// </summary>
    /// <remarks><code>
    /// sub tunnel_dirs {
    ///   my ($dungeon,$last_dir) = @_;
    ///   my $p = $corridor_layout->{$dungeon->{'corridor_layout'}};
    ///   my @dirs = &shuffle(@dj_dirs);
    ///
    ///   if ($last_dir && $p) {
    ///     unshift(@dirs,$last_dir) if (int(rand(100)) < $p);
    ///   }
    ///   return @dirs;
    /// }
    /// </code></remarks>
    /// <param name="dungeon"></param>
    /// <param name="last_dir"></param>
    /// <returns>A shuffled series of directions to consider turning</returns>
    IEnumerable<Cardinal> tunnel_dirs(Dungeon dungeon, Cardinal? last_dir)
    {
        using (logger.BeginScope(nameof(tunnel_dirs)))
        {
            Cardinal[] dirs = [.. DungeonGen.dj_dirs];
            dungeon.random.Shuffle(dirs); // direction keys, but in a random order

            if (corridor_layout.TryGetValue(dungeon.corridor_layout, out var pUncurviness))
            {
                if (last_dir is not null && pUncurviness > 0
                    && dungeon.random.Next(100) < pUncurviness)
                {
                    // prepend lastdir to dirs, so we'll address all the directions, but continue an existing direction first
                    // pUncurviness is the percent chance that we WON'T turn, and will continue in the last dierection
                    return Enumerable.Concat([last_dir.Value], dirs);
                }
            }
            return dirs.AsEnumerable();
        }
    }

    // # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    /// <summary>
    ///  open tunnel
    /// </summary>
    /// <remarks><code>
    /// sub open_tunnel {
    ///   my ($dungeon,$i,$j,$dir) = @_;
    ///   my $this_r = ($i * 2) + 1;
    ///   my $this_c = ($j * 2) + 1;
    ///   my $next_r = (($i + $di->{$dir}) * 2) + 1;
    ///   my $next_c = (($j + $dj->{$dir}) * 2) + 1;
    ///   my $mid_r = ($this_r + $next_r) / 2;
    ///   my $mid_c = ($this_c + $next_c) / 2;
    ///
    ///   if (&sound_tunnel($dungeon,$mid_r,$mid_c,$next_r,$next_c)) {
    ///     return &delve_tunnel($dungeon,$this_r,$this_c,$next_r,$next_c);
    ///   } else {
    ///     return 0;
    ///   }
    /// }
    /// </code></remarks>
    /// <param name="dungeon"></param>
    /// <param name="i">oddrow indexspace</param>
    /// <param name="j">oddcol indexspace</param>
    /// <param name="dir"></param>
    /// <returns>true if the <see cref="delve_tunnel"/> occurred</returns>
    bool open_tunnel(Dungeon dungeon, int i, int j, Cardinal dir)
    {
        // using (logger.BeginScope(nameof(open_tunnel)))
        // {
        // find the current rowspace coordinate
        // r and c will be odd
        (int r, int c) curr = (i * 2 + 1, j * 2 + 1);
        // find the next rowspace coordinate in the proposed direction
        (int r, int c) next = ((i + di[dir]) * 2 + 1, (j + dj[dir]) * 2 + 1);
        // two steps in indicated direction, +one,one
        // the rowspace coordinate between them (because it's a single odd-row increment, the middle should be on the even)
        (int r, int c) mid = ((curr.r + next.r) / 2, (curr.c + next.c) / 2);

        return sound_tunnel(dungeon, mid.r, mid.c, next.r, next.c) && delve_tunnel(dungeon, curr.r, curr.c, next.r, next.c);
        // } // /scope
    }

    /// <summary>
    /// sound tunnel
    /// don't open blocked cells, room perimeters, or other corridors
    /// </summary>
    /// <remarks><code>
    /// sub sound_tunnel {
    ///   my ($dungeon,$mid_r,$mid_c,$next_r,$next_c) = @_;
    ///      return 0 if ($next_r < 0 || $next_r > $dungeon->{'n_rows'});
    ///      return 0 if ($next_c < 0 || $next_c > $dungeon->{'n_cols'});
    ///   my $cell = $dungeon->{'cell'};
    ///   my ($r1,$r2) = sort { $a <=> $b } ($mid_r,$next_r);
    ///   my ($c1,$c2) = sort { $a <=> $b } ($mid_c,$next_c);
    ///
    ///   my $r; for ($r = $r1; $r <= $r2; $r++) {
    ///     my $c; for ($c = $c1; $c <= $c2; $c++) {
    ///       return 0 if ($cell->[$r][$c] & $BLOCK_CORR);
    ///     }
    ///   }
    ///   return 1;
    /// }
    /// </code></remarks>
    /// <param name="dungeon"></param>
    /// <param name="mid_r"></param>
    /// <param name="mid_c"></param>
    /// <param name="next_r"></param>
    /// <param name="next_c"></param>
    /// <returns>true if no cell in the proposed tunnel is Blocked/perimeter/corridor</returns>
    bool sound_tunnel(Dungeon dungeon, int mid_r, int mid_c, int next_r, int next_c)
    {
        using (logger.BeginScope(nameof(sound_tunnel)))
        {
            // reject the proposed tunnel if the destination cell is out of bounds
            if (next_r < 0 || next_r >= dungeon.n_rows) return false;
            if (next_c < 0 || next_c >= dungeon.n_cols) return false;

            // find extents in an order that is convenient for iteration
            int r1 = Math.Min(mid_r, next_r);
            int c1 = Math.Min(mid_c, next_c);
            int r2 = Math.Max(mid_r, next_r);
            int c2 = Math.Max(mid_c, next_c);

            for (int r = r1; r <= r2; r++)
            {
                for (int c = c1; c <= c2; c++)
                {
                    // HasFlag requires the FULL bitmask, not ANY part of it ( x&y !=0 )
                    if (dungeon.cell[r, c].HasAnyFlag(Cellbits.BLOCK_CORR))
                    {
                        logger.LogDebug("tunnel sounding false from {r},{c} to {x},{y}: a block_corr was found", mid_r, mid_c, next_r, next_c);
                        return false;
                    }
                }
            }
            logger.LogDebug("tunnel sounding true from {r},{c} to {x},{y}", mid_r, mid_c, next_r, next_c);
            return true;
        }
    }

    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    /// <summary>
    /// mark all cells in the given inclusive rectangle as <see cref="Cellbits.CORRIDOR"/>, non-<see cref="Cellbits.ENTRANCE"/>
    /// </summary>
    /// <remarks><code>
    /// sub delve_tunnel {
    ///   my ($dungeon,$this_r,$this_c,$next_r,$next_c) = @_;
    ///   my $cell = $dungeon->{'cell'};
    ///   my ($r1,$r2) = sort { $a <=> $b } ($this_r,$next_r);
    ///   my ($c1,$c2) = sort { $a <=> $b } ($this_c,$next_c);
    /// 
    ///   my $r; for ($r = $r1; $r <= $r2; $r++) {
    ///     my $c; for ($c = $c1; $c <= $c2; $c++) {
    ///       $cell->[$r][$c] &= ~ $ENTRANCE;
    ///       $cell->[$r][$c] |= $CORRIDOR;
    ///     }
    ///   }
    ///   return 1;
    /// }
    /// </code></remarks>
    /// <param name="dungeon"></param>
    /// <param name="this_r"></param>
    /// <param name="this_c"></param>
    /// <param name="next_r"></param>
    /// <param name="next_c"></param>
    /// <returns>true, always</returns>
    bool delve_tunnel(Dungeon dungeon, int this_r, int this_c, int next_r, int next_c)
    {
        using (logger.BeginScope("{fn}: delve from ({r},{c}) to ({r2},{c2})",
            nameof(delve_tunnel), this_r, this_c, next_r, next_c))
        {
            int r1 = Math.Min(this_r, next_r);
            int c1 = Math.Min(this_c, next_c);
            int r2 = Math.Max(this_r, next_r);
            int c2 = Math.Max(this_c, next_c);
            for (int r = r1; r <= r2; r++)
            {
                for (int c = c1; c <= c2; c++)
                {
                    logger.LogTrace("delve tunnel: mark {r},{c} as non-entrance corridor", r, c);
                    dungeon.cell[r, c] &= ~Cellbits.ENTRANCE; // filter to everybit EXCEPT entrance (erase Entrance bits)
                    dungeon.cell[r, c] |= Cellbits.CORRIDOR; // set Corridor
                }
            }
            return true;
        }
    }
    #endregion Corridors

    #region Place Stairs

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// <code>
    /// sub emplace_stairs {
    ///   my ($dungeon) = @_;
    ///   my $n = $dungeon->{'add_stairs'};
    ///      return $dungeon unless ($n > 0);
    ///   my @list = &stair_ends($dungeon);
    ///      return $dungeon unless (@list);
    ///   my $cell = $dungeon->{'cell'};
    ///
    ///   my $i; for ($i = 0; $i < $n; $i++) {
    ///     my $stair = splice(@list,int(rand(@list)),1);
    ///        last unless ($stair);
    ///     my $r = $stair->{'row'};
    ///     my $c = $stair->{'col'};
    ///     my $type = ($i < 2) ? $i : int(rand(2));
    ///
    ///     if ($type == 0) {
    ///       $cell->[$r][$c] |= $STAIR_DN;
    ///       $cell->[$r][$c] |= (ord('d') << 24);
    ///       $stair->{'key'} = 'down';
    ///     } else {
    ///       $cell->[$r][$c] |= $STAIR_UP;
    ///       $cell->[$r][$c] |= (ord('u') << 24);
    ///       $stair->{'key'} = 'up';
    ///     }
    ///     push(@{ $dungeon->{'stair'} },$stair);
    ///   }
    ///   return $dungeon;
    /// }
    /// </code>
    /// </remarks>
    /// <param name="dungeon"></param>
    /// <returns></returns>
    Dungeon emplace_stairs(Dungeon dungeon)
    {
        using (logger.BeginScope(nameof(emplace_stairs)))
        {
            var n = dungeon.add_stairs;
            //      return $dungeon unless ($n > 0);
            if (!(n > 0)) return dungeon;
            List<StairEnd?> list = stair_ends(dungeon).ToList();
            if (list is null) return dungeon;

            if (list.Count != n) logger.LogDebug("Select {n} stairs from list of {list}", n, list.Count);
            for (int i = 0; i < n && i < list.Count; i++) //   my $i; for ($i = 0; $i < $n; $i++) {
            {
                //     my $stair = splice(@list,int(rand(@list)),1);
                //? randselect an element of list ,remove, and assign to stair
                int idx = dungeon.random.Next(list.Count);
                StairEnd? stair = list[idx];
                list.RemoveAt(idx);
                //        last unless ($stair);
                //? exit the loop, unless is truthy
                if (stair is null) break;

                (int r, int c) = (stair.row, stair.col);

                var type = i < 2 ? i : dungeon.random.Next(2);
                (char label, Cellbits direction, string key) stairtuple = type switch
                {
                    0 => ('d', Cellbits.STAIR_DN, "down"),
                    _ => ('u', Cellbits.STAIR_DN, "up"),
                };
                stair.key = stairtuple.key;
                dungeon.cell[r, c] |= stairtuple.direction;
                dungeon.cell[r, c] = dungeon.cell[r, c].SetLabel(stairtuple.label);

                logger.LogInformation("AddStair: {s}", stair);
                dungeon.stair.Add(stair);
            }
            if (dungeon.stair.Count != dungeon.add_stairs)
                logger.LogError("exiting {f} with {s} stairs instead of {as}", nameof(emplace_stairs), dungeon.stair.Count, dungeon.add_stairs);
            else
                logger.LogDebug("exiting {f} with {s} stairs from {as} requested", nameof(emplace_stairs), dungeon.stair.Count, dungeon.add_stairs);
            return dungeon;
        }
    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// <summary>
    /// list available ends
    /// </summary>
    /// <code>
    /// sub stair_ends {
    ///   my ($dungeon) = @_;
    ///   my $cell = $dungeon->{'cell'};
    ///   my @list;
    /// 
    ///   my $i; ROW: for ($i = 0; $i < $dungeon->{'n_i'}; $i++) {
    ///       my $r = ($i * 2) + 1;
    ///     my $j; COL: for ($j = 0; $j < $dungeon->{'n_j'}; $j++) {
    ///       my $c = ($j * 2) + 1;
    /// 
    ///       next unless ($cell->[$r][$c] == $CORRIDOR);
    ///       next if ($cell->[$r][$c] & $STAIRS);
    /// 
    ///       my $dir; foreach $dir (keys %{ $stair_end }) {
    ///         if (&check_tunnel($cell,$r,$c,$stair_end->{$dir})) {
    ///           my $end = { 'row' => $r, 'col' => $c };
    ///           my $n = $stair_end->{$dir}{'next'};
    ///              $end->{'next_row'} = $end->{'row'} + $n->[0];
    ///              $end->{'next_col'} = $end->{'col'} + $n->[1];
    /// 
    ///           push(@list,$end); next COL;
    ///         }
    ///       }
    ///     }
    ///   }
    ///   return @list;
    /// }
    /// </code>
    /// <param name="dungeon"></param>
    /// <returns></returns>
    IEnumerable<StairEnd?> stair_ends(Dungeon dungeon)
    {
        using (logger.BeginScope(nameof(stair_ends)))
        {
            List<StairEnd?> list = [];

            //? rowspace/indexspace to enforce odd row/col
            for (int i = 0; i < dungeon.n_i; i++) // lbl ROW
            {
                int r = i * 2 + 1;
                for (int j = 0; j < dungeon.n_j; j++) // lbl COL
                {
                    int c = j * 2 + 1;
                    // r,c reconstituted (as every-other?)

                    //::       next unless ($cell->[$r][$c] == $CORRIDOR);
                    if (dungeon.cell[r, c] != Cellbits.CORRIDOR) continue;
                    //::       next if ($cell->[$r][$c] & $STAIRS);
                    if (dungeon.cell[r, c].HasAnyFlag(Cellbits.STAIRS)) continue;

                    foreach (Cardinal dir in stair_end.Keys)
                    {
                        if (check_tunnel(dungeon.cell, r, c, stair_end[dir]))
                        {
                            StairEnd end = new() { row = r, col = c };
                            (int, int) n = stair_end[dir]["next"].Single();
                            end.next_row = end.row + n.Item1;
                            end.next_col = end.col + n.Item2;

                            //::           push(@list,$end); next COL;
                            list.Add(end);
                            break; // out of dir's foreach, continue to next col in for-j
                        }
                    }
                }
            }
            return list;
        }
    }
    #endregion Place Stairs

    #region Final Cleanup

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// <code>
    /// sub clean_dungeon {
    ///   my ($dungeon) = @_;
    /// 
    ///   if ($dungeon->{'remove_deadends'}) {
    ///     $dungeon = &remove_deadends($dungeon);
    ///   }
    ///   $dungeon = &fix_doors($dungeon);
    ///   $dungeon = &empty_blocks($dungeon);
    /// 
    ///   return $dungeon;
    /// }
    /// </code>
    /// </remarks>
    /// <param name="dungeon"></param>
    /// <returns></returns>
    Dungeon clean_dungeon(Dungeon dungeon)
    {
        using (logger.BeginScope(nameof(clean_dungeon)))
        {
            logger.LogInformation(1, "request {pct}% dead-end removal", dungeon.remove_deadends);
            dungeon = dungeon.remove_deadends != 0 ? remove_deadends(dungeon) : dungeon;
            logger.LogInformation(2, "dead-end removal finished");
            dungeon = fix_doors(dungeon);
            logger.LogInformation(3, "fixdoor finished");
            dungeon = empty_blocks(dungeon);
            return dungeon;
        }
    }

    /// <summary>remove deadend corridors</summary>
    /// <code>
    /// sub remove_deadends {
    ///   my ($dungeon) = @_;
    ///   my $p = $dungeon->{'remove_deadends'};
    /// 
    ///   return &collapse_tunnels($dungeon,$p,$close_end);
    /// }
    /// </code>
    Dungeon remove_deadends(Dungeon dungeon)
    {
        using (logger.BeginScope(nameof(remove_deadends)))
        {
            return collapse_tunnels(dungeon, dungeon.remove_deadends, close_end);
        }
    }

    /// <summary>collapse tunnels</summary>
    /// <code>
    /// sub collapse_tunnels {
    ///   my ($dungeon,$p,$xc) = @_;
    ///      return $dungeon unless ($p);
    ///   my $all = ($p == 100);
    ///   my $cell = $dungeon->{'cell'};
    ///
    ///   my $i; for ($i = 0; $i < $dungeon->{'n_i'}; $i++) {
    ///       my $r = ($i * 2) + 1;
    ///     my $j; for ($j = 0; $j < $dungeon->{'n_j'}; $j++) {
    ///       my $c = ($j * 2) + 1;
    ///
    ///       next unless ($cell->[$r][$c] & $OPENSPACE);
    ///       next if ($cell->[$r][$c] & $STAIRS);
    ///       next unless ($all || (int(rand(100)) < $p));
    ///
    ///       $dungeon = &collapse($dungeon,$r,$c,$xc);
    ///     }
    ///   }
    ///   return $dungeon;
    /// }
    /// </code>
    /// <param name="dungeon"></param>
    /// <param name="p"></param>
    /// <param name="xc"></param>
    /// <returns></returns>
    Dungeon collapse_tunnels(Dungeon dungeon, double p,
        Dictionary<Cardinal, Dictionary<string, ValueTuple<int, int>[]>> xc)
    {
        using (logger.BeginScope(nameof(collapse_tunnels)))
        {
            if (p is 0) return dungeon; //::      return $dungeon unless ($p);
            bool all = p is 100;

            for (int i = 0; i < dungeon.n_i; i++)
            {
                int r = i * 2 + 1;
                for (int j = 0; j < dungeon.n_j; j++)
                {
                    int c = j * 2 + 1;

                    logger.LogTrace("about to collapse ({r},{c})", r, c);
                    //::       next unless ($cell->[$r][$c] & $OPENSPACE);
                    if (false == dungeon.cell[r, c].HasAnyFlag(Cellbits.OPENSPACE)) continue;
                    //::       next if ($cell->[$r][$c] & $STAIRS);
                    if (dungeon.cell[r, c].HasAnyFlag(Cellbits.STAIRS)) continue;
                    //::       next unless ($all || (int(rand(100)) < $p));
                    if (false == (all || dungeon.random.Next(100) < p)) continue;

                    logger.LogTrace("collapse is not preempted for ({r},{c})", r, c);
                    dungeon = collapse(dungeon, r, c, xc);
                }
            }
            return dungeon;
        }
    }

    ///<summary>relative to (r,c), if the proposed tunnel is valid, handle slated closures</summary>
    ///<code>
    /// sub collapse {
    ///   my ($dungeon,$r,$c,$xc) = @_;
    ///   my $cell = $dungeon->{'cell'};
    ///
    ///   unless ($cell->[$r][$c] & $OPENSPACE) {
    ///     return $dungeon;
    ///   }
    ///   my $dir; foreach $dir (keys %{ $xc }) {
    ///     if (&check_tunnel($cell,$r,$c,$xc->{$dir})) {
    ///       my $p; foreach $p (@{ $xc->{$dir}{'close'} }) {
    ///         $cell->[$r+$p->[0]][$c+$p->[1]] = $NOTHING;
    ///       }
    ///       if ($p = $xc->{$dir}{'open'}) {
    ///         $cell->[$r+$p->[0]][$c+$p->[1]] |= $CORRIDOR;
    ///       }
    ///       if ($p = $xc->{$dir}{'recurse'}) {
    ///         $dungeon = &collapse($dungeon,($r+$p->[0]),($c+$p->[1]),$xc);
    ///       }
    ///     }
    ///   }
    ///   return $dungeon;
    /// }
    ///</code>
    Dungeon collapse(Dungeon dungeon, int r, int c, Dictionary<Cardinal, Dictionary<string, ValueTuple<int, int>[]>> xc)
    {
        using (logger.BeginScope(nameof(collapse)))
        {
            // halt if (r,c) is NOT openspace, require openspace to continue
            //! Unless isopen, return :: return except if open :: return if not open
            if (false == dungeon.cell[r, c].HasAnyFlag(Cellbits.OPENSPACE | Cellbits.ENTRANCE)) //! or-entrance is an alteration vs perl
            {
                logger.LogDebug(1, "stopping, found openspace at {r},{c}", r, c);
                return dungeon;
            }

            foreach (Cardinal dir in xc.Keys)
            {
                if (check_tunnel(dungeon.cell, r, c, xc[dir]))
                {
                    // in the direction, handle slated 'close' cells by returning them to Nothing
                    foreach ((int, int) t in xc[dir]["close"])
                    {
                        (int r, int c) del = (r + t.Item1, c + t.Item2);
                        if (del.r == 4 && del.c == 9)
                        {
                            logger.LogWarning(2, "will set {del} to NOTHING", del);
                        }
                        dungeon.cell[del.r, del.c] = Cellbits.NOTHING;
                    }

                    //! because of a quirk in close_end, 'open' is originally NOT DEFINED AT ALL, but
                    //! it is handled as if it is originally a tuple rather than an enumerable
                    //TODO: is 'open' added to dictionary passed into collapse?
                    // if there is A cell slated to 'open', mark it as corridor
                    if (xc[dir].GetValueOrDefault("open")?.SingleOrDefault() is (int, int) p)
                    {
                        logger.LogDebug(3, "reopening corridor {c}", (r + p.Item1, c + p.Item2));
                        dungeon.cell[r + p.Item1, c + p.Item2] |= Cellbits.CORRIDOR;
                    }

                    //! because of a quirk in close_end, 'recurse' is originally a tuple rather than an enumerable
                    //!, DonjonNET has used a IEnumerable<(,)> instead
                    // if ($p = $xc->{$dir}{'recurse'}) { $dungeon = &collapse($dungeon,($r+$p->[0]),($c+$p->[1]),$xc); }
                    if (xc[dir].GetValueOrDefault("recurse")?.SingleOrDefault() is (int, int) q)
                    {
                        logger.LogDebug(4, "recursecollapse {c}", (r + q.Item1, c + q.Item2));
                        dungeon = collapse(dungeon, r + q.Item1, c + q.Item2, xc);
                    }
                }
                else
                {
                    logger.LogDebug(5, "check_tunnel rejected collapse of {r},{c}", r, c);
                }
            }
            return dungeon;
        }
    }

    /// <summary></summary>
    /// <code>
    /// sub check_tunnel {
    ///   my ($cell,$r,$c,$check) = @_;
    ///   my $list;
    ///
    ///   if ($list = $check->{'corridor'}) {
    ///     my $p; foreach $p (@{ $list }) {
    ///       return 0 unless ($cell->[$r+$p->[0]][$c+$p->[1]] == $CORRIDOR);
    ///     }
    ///   }
    ///   if ($list = $check->{'walled'}) {
    ///     my $p; foreach $p (@{ $list }) {
    ///       return 0 if ($cell->[$r+$p->[0]][$c+$p->[1]] & $OPENSPACE);
    ///     }
    ///   }
    ///   return 1;
    /// }
    /// </code>
    /// <param name="cell"></param>
    /// <param name="r">anchor row within the map, points in <see cref="check"/> are relative to this</param>
    /// <param name="c">anchor col within the map, points in <see cref="check"/> are relative to this</param>
    /// <param name="check"></param>
    /// <returns>true if deletion should occur</returns>
    bool check_tunnel(Cellbits[,] cell, int r, int c, Dictionary<string, ValueTuple<int, int>[]> check)
    {
        using (logger.BeginScope(nameof(check_tunnel)))
        {
            if (check.TryGetValue("corridor", out var list))
            {
                logger.LogTrace(1, "test all {n} listed cells relative to ({r},{c}) are Corridor", list.Length, r, c);
                // TODO: consider a linq
                var linqtest = !list.All(p => cell[r + p.Item1, c + p.Item2] == Cellbits.CORRIDOR);
                logger.LogDebug(2, "check_tunnel({r},{c}): corridor: linqresult={linqtest}", r, c, linqtest);
                // if (!list.All(p => cell[r + p.Item1, c + p.Item2] == Cellbits.CORRIDOR)) return false;
                foreach ((int, int) p in list)
                {
                    // return 0 unless ($cell->[$r+$p->[0]][$c+$p->[1]] == $CORRIDOR);
                    if (cell[r + p.Item1, c + p.Item2] == Cellbits.CORRIDOR)
                    {
                        logger.LogDebug(3, "check_tunnel({r},{c}): corridor: continuing by {p}", r, c, p);
                        continue;
                    }
                    else
                    {
                        logger.LogDebug(3, "check_tunnel({r},{c}): corridor: returning false by {p}", r, c, p);
                        return false;
                    }
                }
            }
            if (check.TryGetValue("walled", out var walledlist))
            {
                logger.LogTrace(4, "test all {n} listed cells relative to ({r},{c}) are not Openspace", walledlist.Length, r, c);
                var linq2 = walledlist
                    .Select(p => cell[r + p.Item1, c + p.Item2])
                    .Any(p => p.HasAnyFlag(Cellbits.OPENSPACE | Cellbits.ENTRANCE));
                logger.LogDebug(5, "check_tunnel({r},{c}): walled: linqresult={linq2}", r, c, linq2);
                // if (linq2) return false;
                foreach ((int, int) p in walledlist)
                {
                    // return 0 if ($cell->[$r+$p->[0]][$c+$p->[1]] & $OPENSPACE);
                    bool reject = cell[r + p.Item1, c + p.Item2].HasAnyFlag(Cellbits.OPENSPACE | Cellbits.ENTRANCE); //! or-entrance is an alteration vs perl
                    logger.LogDebug(6, "check_tunnel({r},{c}): corridor: reject {reject} by {p}", r, c, reject, p);
                    if (reject) return false;
                }
            }
            logger.LogDebug(7, "corridor check from ({r},{c}) is OK", r, c);
            return true;
        }
    }

    /// <summary>fix door lists</summary>
    /// <remarks><code>
    /// sub fix_doors {
    ///   my ($dungeon) = @_;
    ///   my $cell = $dungeon->{'cell'};
    ///   my $fixed;
    ///
    ///   my $room; foreach $room (@{ $dungeon->{'room'} }) {
    ///     my $dir; foreach $dir (sort keys %{ $room->{'door'} }) {
    ///       my ($door,@shiny); foreach $door (@{ $room->{'door'}{$dir} }) {
    ///         my $door_r = $door->{'row'};
    ///         my $door_c = $door->{'col'};
    ///         my $door_cell = $cell->[$door_r][$door_c];
    ///            next unless ($door_cell & $OPENSPACE);
    ///
    ///         if ($fixed->[$door_r][$door_c]) {
    ///           push(@shiny,$door);
    ///         } else {
    ///           my $out_id; if ($out_id = $door->{'out_id'}) {
    ///             my $out_dir = $opposite->{$dir};
    ///             push(@{ $dungeon->{'room'}[$out_id]{'door'}{$out_dir} },$door);
    ///           }
    ///           push(@shiny,$door);
    ///           $fixed->[$door_r][$door_c] = 1;
    ///         }
    ///       }
    ///       if (@shiny) {
    ///         $room->{'door'}{$dir} = \@shiny;
    ///         push(@{ $dungeon->{'door'} },@shiny);
    ///       } else {
    ///         delete $room->{'door'}{$dir};
    ///       }
    ///     }
    ///   }
    ///   return $dungeon;
    /// }
    /// </code></remarks>
    Dungeon fix_doors(Dungeon dungeon)
    {
        using (logger.BeginScope(nameof(fix_doors)))
        {
            try
            {
                logger.LogInformation(nameof(fix_doors));
                var alldoors = dungeon.room.SelectMany(r => r.Value.door.SelectMany(d => d.Value));
                var shinydoors = alldoors.Where(d => dungeon.cell[d.row, d.col].HasAnyFlag(Cellbits.OPENSPACE | Cellbits.ENTRANCE)); //! or-entrance is an alteration vs perl
                // var shinydoors = alldoors.Where(d => dungeon.cell[d.row, d.col].HasAnyFlag(Cellbits.OPENSPACE));
                ///            next unless ($door_cell & $OPENSPACE);
                //? var shinydoors = alldoors.Where(d => dungeon.cell[d.row, d.col].HasAnyFlag(Cellbits.OPENSPACE|Cellbits.ENTRANCE));

                var delta = alldoors.ExceptBy<DoorData, (int, int)>(second: shinydoors.Select(o => o.Coord), keySelector: ko => ko.Coord);

                var groupings = shinydoors.GroupBy(keySelector: door => (door.row, door.col));
                if (groupings.Any(g => g.Count() > 1))
                    throw new InvalidOperationException("Indications of non-unique doors");
                var doorSupplement = groupings.Select(g => g.First());

                logger.LogInformation("Got doors of each room to a list of {a}, and filtered to {s}, dedup'd to {g}",
                    alldoors.Count(), shinydoors.Count(), doorSupplement.Count());
                dungeon.door.AddRange(doorSupplement);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "fixdoors issue");
                throw;
            }
        }
        return dungeon;
    }

    /// <summary>Turn Blocked cells into Nothing cells
    /// <code>
    /// sub empty_blocks {
    ///   my ($dungeon) = @_;
    ///   my $cell = $dungeon->{'cell'};
    /// 
    ///   my $r; for ($r = 0; $r <= $dungeon->{'n_rows'}; $r++) {
    ///     my $c; for ($c = 0; $c <= $dungeon->{'n_cols'}; $c++) {
    ///       $cell->[$r][$c] = $NOTHING if ($cell->[$r][$c] & $BLOCKED);
    ///     }
    ///   }
    ///   return $dungeon;
    /// }
    /// </code>
    /// </summary>
    /// <remarks>only works because initcells is inclusive!
    /// TODO: convert to inclusive logic around n_row/col
    /// </remarks>
    Dungeon empty_blocks(Dungeon dungeon)
    {
        logger.LogInformation(nameof(empty_blocks));
        dungeon.ForeachInclusive((r, c) =>
        {
            if (dungeon.cell[r, c].HasAnyFlag(Cellbits.BLOCKED)) dungeon.cell[r, c] = Cellbits.NOTHING;
        });
        return dungeon;
    }

    #endregion Final Cleanup

    public string DescribeDungeon(Dungeon d, Func<Cellbits, int, int, string> cellFormatter, string separator = " ", bool preamble = true)
    {
        System.Text.StringBuilder b = new();
        if (preamble)
        {
            b.AppendLine($"seed:{d.seed} {d.n_rows}x{d.n_cols} csz={d.cell_size} dun{d.dungeon_layout} cor{d.corridor_layout}");
            b.AppendLine($"nrooms:{d.n_rooms} actual:{d.room.Count} last='{d.last_room_id?.ToString() ?? "nul"}' sz({d.room_min}..{d.room_max})");
            foreach (var item in d.room)
            {
                b.Append($"    key'{item.Key}' [id{item.Value.id}] | ({item.Value.north},{item.Value.west})..({item.Value.south},{item.Value.east})");
                b.AppendLine($" | {item.Value.height}v x {item.Value.width}h = {item.Value.area}");
            }


            b.AppendLine($"ndoor:{d.door.Count}");
            foreach (var item in d.door.OrderBy(dr => dr.col).OrderBy(dr => dr.row))
            {
                b.AppendLine($"    ({item.row,3},{item.col,3}) {item.key} oid{item.out_id?.ToString() ?? "_"} {item.type} :'{item.desc}' :: {d.cell[item.row, item.col] & ~Cellbits.LABELSPACE}");
            }

            b.AppendLine($"nStair:{d.stair.Count}");
            foreach (var item in d.stair)
            {
                b.AppendLine(item is null ? "    null" : $"    ({item.row},{item.col}) {item.key} next:({item.next_row},{item.next_col})");
            }
        }
        b.AppendLine($"        {string.Join(separator, Enumerable.Range(0, d.n_cols).Select(c => c % 10))}");
        for (int r = 0; r < d.cell.GetLength(0); r++)
        {
            b.AppendFormat("{0,4}:: [", r);
            var line = Enumerable.Range(0, d.cell.GetLength(1))
                .Select(c => cellFormatter(d.cell[r, c], r, c));
            b.AppendJoin(separator, line);
            b.AppendLine("]");
        }
        return b.ToString();
    }

    public string DescribeDungeon(Dungeon d, int size = 3)
        => DescribeDungeon(d, (cel, r, c) => string.Format($"[{{0,{size}}}]", cel.Summarize()));

    public string DescribeDungeonLite(IDungeon dungeon) => dungeon is Dungeon d ? DescribeDungeon(d,
        cellFormatter: (cel, r, c) => cel switch
        {
            Cellbits d when d.HasAnyFlag(Cellbits.DOORSPACE)
                && dungeon.door.Any(door => door.Coord == (r, c)) => "D",//"D",
            Cellbits d when d.HasAnyFlag(Cellbits.DOORSPACE) => "Ð",//"D",
            Cellbits d when d.HasAnyFlag(Cellbits.STAIR_UP) => "^",
            Cellbits d when d.HasAnyFlag(Cellbits.STAIR_DN) => "v",
            Cellbits d when d.HasAnyFlag(Cellbits.ROOM) => "·",
            Cellbits d when d.HasAnyFlag(Cellbits.CORRIDOR) => "+",//"◇",
            Cellbits d when d.HasAnyFlag(Cellbits.ENTRANCE) => "E",
            Cellbits d when d.HasAnyFlag(Cellbits.BLOCKED) => "x",
            Cellbits d when d.HasAnyFlag(Cellbits.PERIMETER) => "#",//"⨂",
            Cellbits d when d == Cellbits.NOTHING => " ",
            _ => "?"
        }, preamble: true) : throw new ArgumentException("not a Dungeon");

    public string IndicatePosition(Dungeon d, int i, int j)
        => DescribeDungeon(d, (cel, r, c) => r == i && c == j ? "X" : "•", preamble: false);
}
#pragma warning restore IDE1006 // Naming Styles

public enum Cardinal
{
    //north, south, east, west 
    east, north, south, west
    //  e,n,s,w
}