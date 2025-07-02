using System.Text.Json;

using Donjon.Original;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using SixLabors.ImageSharp;

namespace Donjon;
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
/// <typeparam name="DungeonGenRefactored"></typeparam>
public partial class DungeonGenRefactored(
    IOptions<Settings> settings,
    ILoggerFactory loggerFactory) : IDungeonGenerator
{
    private ILogger<DungeonGenRefactored> logger { get; } = loggerFactory?.CreateLogger<DungeonGenRefactored>()
        ?? NullLogger<DungeonGenRefactored>.Instance;
    public IDungeonRoomIssuer RoomIssuer => MyRoomIssuer;
    public RoomIdIssuer MyRoomIssuer { get; } = new RoomIdIssuer(Options.Create(settings.Value.Rooms), Options.Create(settings.Value.Dungeon));
    public IDungeon Create_dungeon()
    {
        return Settings.CreateLegacy(settings.Value);
    }
    private static readonly JsonSerializerOptions jsonLoggingOptions = new() { WriteIndented = false };

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
    public IDungeon Create_dungeon(Dungeon dungeon)
    {
        using (logger.BeginScope(nameof(Create_dungeon)))
        {
            dungeon = init_cells(dungeon);
            logger.LogInformation(1, "dungeonState {s}", _w.DescribeDungeon(dungeon));

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
        logger.LogInformation(8, "dungeonState {s}", _w.DescribeDungeon(dungeon));
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

    /// <summary>
    /// stretching <paramref name="mask"/> to fit the cell field, apply mask as BLOCKED
    /// </summary>
    /// <param name="dungeon"></param>
    /// <param name="mask"></param>
    /// <returns></returns>
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
            var r_x = mask.GetLength(0) * 1 / (dungeon.n_rows + 1); //::   my $r_x = (scalar @{ $mask } * 1.0 / ($dungeon->{'n_rows'} + 1));
            var c_x = mask.GetLength(1) * 1 / (dungeon.n_cols + 1); //::   my $c_x = (scalar @{ $mask->[0] } * 1.0 / ($dungeon->{'n_cols'} + 1));
            //? should transpose getlen index?

            //RASTER: REALSPACE: INCLUSIVE <0,0>..<nrows,ncols>
            foreach (var (r, c) in Dim2d.RangeInclusive(0, dungeon.n_rows, 0, dungeon.n_cols))
            {
                //::       $cell->[$r][$c] = $BLOCKED unless ($mask->[$r * $r_x][$c * $c_x]);
                dungeon.cell[r, c] = (mask[r * r_x, c * c_x] != 0) ? dungeon.cell[r, c] : Cellbits.BLOCKED;
            }
            return dungeon;
        }
    }

    /// <summary>
    /// Mark any cell outside a circle as BLOCKED
    /// </summary>
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
            var centerOfMap = (r: dungeon.n_rows / 2, c: dungeon.n_cols / 2);

            //RASTER: REALSPACE: INCLUSIVE <0,0>..<nrows,ncols>
            foreach (var (r, c) in Dim2d.RangeInclusive(0, dungeon.n_rows, 0, dungeon.n_cols))
            {
                var radius = Math.Sqrt(Math.Pow(r - centerOfMap.r, 2) + Math.Pow(c - centerOfMap.c, 2));
                //       $cell->[$r][$c] = $BLOCKED if ($d > $center_c);
                dungeon.cell[r, c] = (radius > centerOfMap.c) ? Cellbits.BLOCKED : dungeon.cell[r, c];
            }
            return dungeon;
        }
    }
    #endregion initialize cells

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
            for (int stairIndex = 0; stairIndex < n && stairIndex < list.Count; stairIndex++) //   my $i; for ($i = 0; $i < $n; $i++) {
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

                var type = stairIndex < 2 ? stairIndex : dungeon.random.Next(2);
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
            //RASTER: HEMI: EXCLUSIVE-high [<0,0>..<ni=nrows/2(E),nj=ncols/2(E)>)
            foreach (var (i, j) in Dim2d.RangeInclusive(0, dungeon.n_i - 1, 0, dungeon.n_j - 1).AsHemi())
            {
                // r,c reconstituted (as every-other? odd)
                var (r, c) = (i, j).ToRealspace();

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

            //RASTER: HEMI: ExCLUSIVE High  [<0,0> .. <ni=nrows/2-1(o),nj=ncols/2-1(o)]
            foreach (var (r, c) in Dim2d.RangeInclusive(0, dungeon.n_i - 1, 0, dungeon.n_j - 1)
                .AsHemi().Select(ij => ij.ToRealspace()))
            {
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
                        if (del.r == 4 && del.c == 9) //? was this a debug trace?
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

        foreach ((var r, var c) in Dim2d.RangeInclusive(0, dungeon.max_row, 0, dungeon.max_col)
            .Where(rc => dungeon.cell[rc.r, rc.c].HasAnyFlag(Cellbits.BLOCKED)))
        {
            dungeon.cell[r, c] = Cellbits.NOTHING;
        }

        // dungeon.ForeachInclusive((r, c) =>
        // {
        //     if (dungeon.cell[r, c].HasAnyFlag(Cellbits.BLOCKED)) dungeon.cell[r, c] = Cellbits.NOTHING;
        // });
        return dungeon;
    }

    #endregion Final Cleanup

    private DungeonWriter _w = new();
    public string DescribeDungeonLite(IDungeon dungeon)=> _w.DescribeDungeonLite(dungeon);
    public string IndicatePosition(Dungeon d, int i, int j)=>_w.IndicatePosition(d, i, j);
}
#pragma warning restore IDE1006 // Naming Styles
