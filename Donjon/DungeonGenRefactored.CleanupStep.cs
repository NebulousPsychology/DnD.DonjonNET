using System.Diagnostics.CodeAnalysis;

using Donjon.Original;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SixLabors.ImageSharp;

namespace Donjon;
#pragma warning disable IDE1006 // Naming Styles

public partial class DungeonGenRefactored
{
    public class CleanupStep(ILogger<CleanupStep> logger, IOptions<Settings> settings, ILoggerFactory loggerFactory, Random random, DungeonWriter dungeonWriter) : PruningStepBase(logger)
    {
        public override bool TryInvoke(IDungeon input, [MaybeNullWhen(false), NotNullWhen(true)] out IDungeon? result)
        {
            result = clean_dungeon(input);
            return result is not null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dungeon"></param>
        /// <returns></returns>
        IDungeon clean_dungeon(IDungeon dungeon)
        {
            /*
            //:: sub clean_dungeon {
            //::   my ($dungeon) = @_;
            //:: 
            //::   if ($dungeon->{'remove_deadends'}) {
            //::     $dungeon = &remove_deadends($dungeon);
            //::   }
            //::   $dungeon = &fix_doors($dungeon);
            //::   $dungeon = &empty_blocks($dungeon);
            //:: 
            //::   return $dungeon;
            //:: }
            */
            using (logger.BeginScope(nameof(clean_dungeon)))
            {
                var pipe = new Pipeline.Pipeline<IDungeon>(loggerFactory.CreateLogger<Pipeline.Pipeline<IDungeon>>(), nameof(clean_dungeon))
                .RegisterStep(settings.Value.Corridors.remove_deadends != 0 ? remove_deadends : d => d)
                .RegisterStep(fix_doors)
                .RegisterStep(empty_blocks);
                return pipe.TryInvoke(dungeon, out var result) ? result : throw new Exception(nameof(clean_dungeon));
            }
        }

        /// <summary>remove deadend corridors</summary> 
        IDungeon remove_deadends(IDungeon dungeon)
        {
            /*
            //:: sub remove_deadends {
            //::   my ($dungeon) = @_;
            //::   my $p = $dungeon->{'remove_deadends'};
            //:: 
            //::   return &collapse_tunnels($dungeon,$p,$close_end);
            //:: }
            */
            using (logger.BeginScope(nameof(remove_deadends)))
            {
                try
                {
                    logger.LogDebug("collapsing");
                    return collapse_tunnels(dungeon, settings.Value.Corridors.remove_deadends, close_end2);
                }
                catch (Exception any)
                {
                    logger.LogError(any, "remove deadentds {e}", any);
                    throw;
                }
            }
        }

        /// <summary>collapse tunnels: across every hemicell's realspace counterpart, 
        /// find excuses to skip, 
        /// otherwise, <see cref="collapse"/> the cell </summary>
        /// <param name="dungeon"></param>
        /// <param name="p"></param>
        /// <param name="xc"></param>
        /// <returns></returns>
        IDungeon collapse_tunnels(IDungeon dungeon, double p,
            Dictionary<Cardinal, CloseWallingArguments> xc)
        {
            /*
            //:: sub collapse_tunnels {
            //::   my ($dungeon,$p,$xc) = @_;
            //::      return $dungeon unless ($p);
            //::   my $all = ($p == 100);
            //::   my $cell = $dungeon->{'cell'};
            //::
            //::   my $i; for ($i = 0; $i < $dungeon->{'n_i'}; $i++) {
            //::       my $r = ($i * 2) + 1;
            //::     my $j; for ($j = 0; $j < $dungeon->{'n_j'}; $j++) {
            //::       my $c = ($j * 2) + 1;
            //::
            //::       next unless ($cell->[$r][$c] & $OPENSPACE);
            //::       next if ($cell->[$r][$c] & $STAIRS);
            //::       next unless ($all || (int(rand(100)) < $p));
            //::
            //::       $dungeon = &collapse($dungeon,$r,$c,$xc);
            //::     }
            //::   }
            //::   return $dungeon;
            //:: }
            */
            using (logger.BeginScope(nameof(collapse_tunnels)))
            {
                if (p is 0) return dungeon; //::      return $dungeon unless ($p);
                bool all = p is 100;
                logger.LogDebug("ping");
                //RASTER: HEMI: ExCLUSIVE High  [<0,0> .. <ni=nrows/2-1(o),nj=ncols/2-1(o)]
                foreach (var rc in Dim2d.RangeInclusive(0, dungeon.n_i - 1, 0, dungeon.n_j - 1)
                    .AsHemi().Select(ij => ij.ToRealspace()))
                {
                    logger.LogTrace("about to collapse ({rc})", rc);
                    //::       next unless ($cell->[$r][$c] & $OPENSPACE);
                    if (false == dungeon.cell.Get(rc).HasAnyFlag(Cellbits.OPENSPACE)) continue;
                    //::       next if ($cell->[$r][$c] & $STAIRS);
                    if (dungeon.cell.Get(rc).HasAnyFlag(Cellbits.STAIRS)) continue;
                    //::       next unless ($all || (int(rand(100)) < $p));
                    if (false == (all || random.Next(100) < p)) continue;

                    logger.LogTrace("collapse is not preempted for ({rc})", rc);
                    dungeon = collapse(dungeon, new(rc), xc);
                }
                logger.LogDebug("finished collapse with\n {ddl}", dungeonWriter.DescribeDungeonLite(dungeon));
            }
            return dungeon;
        }

        ///<summary>relative to (r,c), if the proposed tunnel is valid, handle slated closures</summary> 
        /// <param name="dungeon"></param>
        /// <param name="rc"></param>
        /// <param name="xc">closure patterns</param>
        /// <returns></returns>
        IDungeon collapse(IDungeon dungeon, Realspace<Coord> rc, Dictionary<Cardinal, CloseWallingArguments> xc)
        {
            /*
            //:: sub collapse {
            //::   my ($dungeon,$r,$c,$xc) = @_;
            //::   my $cell = $dungeon->{'cell'};
            //::
            //::   unless ($cell->[$r][$c] & $OPENSPACE) {
            //::     return $dungeon;
            //::   }
            //::   my $dir; foreach $dir (keys %{ $xc }) {
            //::     if (&check_tunnel($cell,$r,$c,$xc->{$dir})) {
            //::       my $p; foreach $p (@{ $xc->{$dir}{'close'} }) {
            //::         $cell->[$r+$p->[0]][$c+$p->[1]] = $NOTHING;
            //::       }
            //::       if ($p = $xc->{$dir}{'open'}) {
            //::         $cell->[$r+$p->[0]][$c+$p->[1]] |= $CORRIDOR;
            //::       }
            //::       if ($p = $xc->{$dir}{'recurse'}) {
            //::         $dungeon = &collapse($dungeon,($r+$p->[0]),($c+$p->[1]),$xc);
            //::       }
            //::     }
            //::   }
            //::   return $dungeon;
            //:: }
            */
            // FIXME: messy
            using (logger.BeginScope(nameof(collapse)))
            {
                // Halt if (r,c) is NOT openspace, require openspace to continue
                //::   unless ($cell->[$r][$c] & $OPENSPACE) { return $dungeon; }
                if (false == dungeon.cell.Get(rc).HasAnyFlag(Cellbits.OPENSPACE | Cellbits.ENTRANCE)) //! or-entrance is an alteration vs perl
                {
                    return dungeon;
                }

                //:: my $dir; foreach $dir (keys %{ $xc })
                foreach (Cardinal dir in xc.Keys)
                {
                    //::    if (&check_tunnel($cell,$r,$c,$xc->{$dir})) {...}
                    if (check_tunnel(dungeon.cell, rc, xc[dir]))
                    {
                        //::        my $p; foreach $p (@{ $xc->{$dir}{'close'} }) 
                        //::        {
                        //::            $cell->[$r+$p->[0]][$c+$p->[1]] = $NOTHING;
                        //::        }
                        // in the direction, handle slated 'close' cells by returning them to Nothing
                        foreach (Coord closeOffset in xc[dir].Close)
                        {
                            dungeon.cell.Set(rc + closeOffset, Cellbits.NOTHING);
                        }

#if CLOSEWALLING_HAS_OPEN
                    //::        if ($p = $xc->{$dir}{'open'}) 
                    //::        {
                    //::            $cell->[$r+$p->[0]][$c+$p->[1]] |= $CORRIDOR;
                    //::        }
                    //! because of a quirk in close_end, 'open' is originally NOT DEFINED AT ALL, but
                    //! it is handled as if it is originally a tuple rather than an enumerable
                    //TODO: is 'open' added to dictionary passed into collapse?
                    // if there is A cell slated to 'open', mark it as corridor
                    if (xc[dir].getValueOrDefault("open")?.SingleOrDefault() is (int, int) p)
                    {
                        var openPt = rc + p;
                        logger.LogDebug(3, "reopening corridor {c}", openPt);
                        dungeon.cell.Set(openPt, c => c |= Cellbits.CORRIDOR);
                    }
#endif

                        //::        if ($p = $xc->{$dir}{'recurse'}) 
                        //::        {
                        //::            $dungeon = &collapse($dungeon,($r+$p->[0]),($c+$p->[1]),$xc);
                        //::        }
                        //! because of a quirk in close_end, 'recurse' is originally a tuple rather than an enumerable
                        //!, DonjonNET has used a IEnumerable<(,)> instead
                        // if ($p = $xc->{$dir}{'recurse'}) { $dungeon = &collapse($dungeon,($r+$p->[0]),($c+$p->[1]),$xc); }
                        if (xc[dir].Next is Coord q) //FIXME: always true? Go ahead to collapse at the Next cell
                        {
                            dungeon = collapse(dungeon, rc + q, xc);
                        }
                    }
                    else
                    {
                    }
                }
                return dungeon;
            }
        }

        /// <summary>fix door lists
        /// </summary>
        IDungeon fix_doors(IDungeon dungeon)
        {
            /*
            //:: sub fix_doors {
            //::   my ($dungeon) = @_;
            //::   my $cell = $dungeon->{'cell'};
            //::   my $fixed;
            //::
            //::   my $room; foreach $room (@{ $dungeon->{'room'} }) {
            //::     my $dir; foreach $dir (sort keys %{ $room->{'door'} }) {
            //::       my ($door,@shiny); foreach $door (@{ $room->{'door'}{$dir} }) {
            //::         my $door_r = $door->{'row'};
            //::         my $door_c = $door->{'col'};
            //::         my $door_cell = $cell->[$door_r][$door_c];
            //::            next unless ($door_cell & $OPENSPACE);
            //::
            //::         if ($fixed->[$door_r][$door_c]) {
            //::           push(@shiny,$door);
            //::         } else {
            //::           my $out_id; if ($out_id = $door->{'out_id'}) {
            //::             my $out_dir = $opposite->{$dir};
            //::             push(@{ $dungeon->{'room'}[$out_id]{'door'}{$out_dir} },$door);
            //::           }
            //::           push(@shiny,$door);
            //::           $fixed->[$door_r][$door_c] = 1;
            //::         }
            //::       }
            //::       if (@shiny) {
            //::         $room->{'door'}{$dir} = \@shiny;
            //::         push(@{ $dungeon->{'door'} },@shiny);
            //::       } else {
            //::         delete $room->{'door'}{$dir};
            //::       }
            //::     }
            //::   }
            //::   return $dungeon;
            //:: }
            */
            using (logger.BeginScope(nameof(fix_doors)))
            {
                try
                {
                    /* with explanation notes
                    //::    my $room; foreach $room (@{ $dungeon->{'room'} }) {
                    //::        my $dir; foreach $dir (sort keys %{ $room->{'door'} }) {
                    //::            my ($door,@shiny); foreach $door (@{ $room->{'door'}{$dir} }) {
                    ///                 // ^for every doordata of every room
                    //::                my $door_r = $door->{'row'};
                    //::                my $door_c = $door->{'col'};
                    //::                my $door_cell = $cell->[$door_r][$door_c];
                    //::                
                    //::                next unless ($door_cell & $OPENSPACE);
                    //                  if cell is not openspace, continue
                    //                  // so, above was aka: for every doordata... where doorcell is openspace
                    //::                
                    //::                if ($fixed->[$door_r][$door_c]) {
                    //                      // rc is 'fixed', so push door to shiny
                    //::                    push(@shiny,$door);
                    //::                } else {
                    //                      // otherwise if the door has an out_id, (if the door needs )
                    //::                    my $out_id; if ($out_id = $door->{'out_id'}) {
                    //?                     // if (door.trygetvalue('out_id',out var out_id)) ...
                    //                          // where 'stuff' is: when teh door has an id:
                    //                          // get room by doorid
                    //                          // get oppositedirection
                    //                          // init room's door list for the opposing dir, if needed
                    //                          // add the door to the opposingdir room's doors list
                    //::                        my $out_dir = $opposite->{$dir};
                    //::                        push(@{ $dungeon->{'room'}[$out_id]{'door'}{$out_dir} },$door);
                    //::                    } // /hasid
                    //                      // add the shiny door, and mark the cell 'fixed'
                    //::                    push(@shiny,$door);
                    //::                    $fixed->[$door_r][$door_c] = 1;
                    //::                } // /else unfixed
                    //::            } // /each door
                    // if any shiny,  set shiny as the room's doors list for direction, and 
                    //::            if (@shiny) {
                    //                  // add shiny to the dungeon's master door list
                    //::                $room->{'door'}{$dir} = \@shiny;
                    //::                push(@{ $dungeon->{'door'} },@shiny);
                    //::            } else {
                    //                  // otherwise, remove direction from room's Doors
                    //::                delete $room->{'door'}{$dir};
                    //::            } //  /ifshiny
                    //  then, we're done with this dir
                    //::        } // /each dir
                    //  then, we're done with this room
                    //::   } // /each room
                    */
                    logger.LogInformation(nameof(fix_doors));
                    IEnumerable<DoorData> alldoors = dungeon.room.SelectMany(r => r.Value.door.SelectMany(d => d.Value));

                    // a doordata is 'shiny' if its cell is OPENSPACE (or entrance)
                    //! or-entrance is an alteration vs perl
                    var shinydoors = alldoors.Where(d => dungeon.cell[d.row, d.col]
                        .HasAnyFlag(Cellbits.OPENSPACE | Cellbits.ENTRANCE));

                    var delta = alldoors.ExceptBy<DoorData, Coord>(
                        second: shinydoors.Select(o => (Coord)o.Coord),
                        keySelector: ko => ko.Coord);

                    var groupings = shinydoors.GroupBy(keySelector: door => (door.row, door.col));
                    if (groupings.Any(g => g.Count() > 1))
                        throw new InvalidOperationException("Indications of non-unique doors");
                    var doorSupplement = groupings.Select(g => g.First());

                    logger.LogInformation("Got doors of each room to a list of {a}, and filtered to {s}, dedup'd to {g}",
                        alldoors.Count(), shinydoors.Count(), doorSupplement.Count());
                    foreach (var ds in doorSupplement)
                        dungeon.door.Add(ds);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "fixdoors issue");
                    throw;
                }
            }
            return dungeon;
        }

        /// <summary>
        /// Turn Blocked cells into Nothing cells
        /// </summary>
        /// <remarks>only works because initcells is inclusive!
        /// TODO: convert to inclusive logic around n_row/col
        /// </remarks>
        IDungeon empty_blocks(IDungeon dungeon)
        {
            /*
            //:: sub empty_blocks {
            //::   my ($dungeon) = @_;
            //::   my $cell = $dungeon->{'cell'};
            //:: 
            //::   my $r; for ($r = 0; $r <= $dungeon->{'n_rows'}; $r++) {
            //::     my $c; for ($c = 0; $c <= $dungeon->{'n_cols'}; $c++) {
            //::       $cell->[$r][$c] = $NOTHING if ($cell->[$r][$c] & $BLOCKED);
            //::     }
            //::   }
            //::   return $dungeon;
            //:: }
            */
            logger.LogInformation(nameof(empty_blocks));

            foreach ((var r, var c) in Dim2d.RangeInclusive(0, dungeon.max_row, 0, dungeon.max_col)
                .Where(rc => dungeon.cell[rc.r, rc.c].HasAnyFlag(Cellbits.BLOCKED)))
            {
                dungeon.cell[r, c] = Cellbits.NOTHING;
            }
            return dungeon;
        }

    }
}
#pragma warning restore IDE1006 // Naming Styles
