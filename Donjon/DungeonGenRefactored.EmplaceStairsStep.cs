using System.Diagnostics.CodeAnalysis;

using Donjon.Original;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Donjon;
#pragma warning disable IDE1006 // Naming Styles

public partial class DungeonGenRefactored
{
    public class EmplaceStairsStep(ILogger<EmplaceStairsStep> logger, IOptions<Settings> settings, Random random) : PruningStepBase(logger)
    {
        public override bool TryInvoke(IDungeon input, [MaybeNullWhen(false), NotNullWhen(true)] out IDungeon? result)
        {
            result = settings.Value.Corridors.add_stairs is > 0 ? emplace_stairs(input) : input;
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dungeon"></param>
        /// <returns></returns>
        IDungeon emplace_stairs(IDungeon dungeon)
        {
            /*
            //:: sub emplace_stairs {
            //::   my ($dungeon) = @_;
            //::   my $n = $dungeon->{'add_stairs'};
            //::      return $dungeon unless ($n > 0);
            //::   my @list = &stair_ends($dungeon);
            //::      return $dungeon unless (@list);
            //::   my $cell = $dungeon->{'cell'};
            //::
            //::   my $i; for ($i = 0; $i < $n; $i++) {
            //::     my $stair = splice(@list,int(rand(@list)),1);
            //::        last unless ($stair);
            //::     my $r = $stair->{'row'};
            //::     my $c = $stair->{'col'};
            //::     my $type = ($i < 2) ? $i : int(rand(2));
            //::
            //::     if ($type == 0) {
            //::       $cell->[$r][$c] |= $STAIR_DN;
            //::       $cell->[$r][$c] |= (ord('d') << 24);
            //::       $stair->{'key'} = 'down';
            //::     } else {
            //::       $cell->[$r][$c] |= $STAIR_UP;
            //::       $cell->[$r][$c] |= (ord('u') << 24);
            //::       $stair->{'key'} = 'up';
            //::     }
            //::     push(@{ $dungeon->{'stair'} },$stair);
            //::   }
            //::   return $dungeon;
            //:: }
            */
            using (logger.BeginScope(nameof(emplace_stairs)))
            {
                var n = settings.Value.Corridors.add_stairs;
                //      return $dungeon unless ($n > 0);
                if (!(n > 0)) return dungeon;
                List<StairEnd?> list = [.. stair_ends(dungeon)];
                if (list is null) return dungeon;

                if (list.Count != n) logger.LogDebug("Select {n} stairs from list of {list}", n, list.Count);
                for (int stairIndex = 0; stairIndex < n && stairIndex < list.Count; stairIndex++) //   my $i; for ($i = 0; $i < $n; $i++) {
                {
                    //     my $stair = splice(@list,int(rand(@list)),1);
                    //? randselect an element of list ,remove, and assign to stair
                    int idx = random.Next(list.Count);
                    StairEnd? stair = list[idx];
                    list.RemoveAt(idx);
                    //        last unless ($stair);
                    //? exit the loop, unless is truthy
                    if (stair is null) break;

                    (int r, int c) = (stair.row, stair.col);

                    var type = stairIndex < 2 ? stairIndex : random.Next(2);
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
                if (dungeon.stair.Count != settings.Value.Corridors.add_stairs)
                    logger.LogError("exiting {f} with {s} stairs instead of {as}", nameof(emplace_stairs), dungeon.stair.Count, settings.Value.Corridors.add_stairs);
                else
                    logger.LogDebug("exiting {f} with {s} stairs from {as} requested", nameof(emplace_stairs), dungeon.stair.Count, settings.Value.Corridors.add_stairs);
                return dungeon;
            }
        }

        /// <summary>
        /// list available ends
        /// </summary>
        /// <param name="dungeon"></param>
        /// <returns></returns>
        IEnumerable<StairEnd?> stair_ends(IDungeon dungeon)
        {
            /*
            //:: sub stair_ends {
            //::   my ($dungeon) = @_;
            //::   my $cell = $dungeon->{'cell'};
            //::   my @list;
            //:: 
            //::   my $i; ROW: for ($i = 0; $i < $dungeon->{'n_i'}; $i++) {
            //::       my $r = ($i * 2) + 1;
            //::     my $j; COL: for ($j = 0; $j < $dungeon->{'n_j'}; $j++) {
            //::       my $c = ($j * 2) + 1;
            //:: 
            //::       next unless ($cell->[$r][$c] == $CORRIDOR);
            //::       next if ($cell->[$r][$c] & $STAIRS);
            //:: 
            //::       my $dir; foreach $dir (keys %{ $stair_end }) {
            //::         if (&check_tunnel($cell,$r,$c,$stair_end->{$dir})) {
            //::           my $end = { 'row' => $r, 'col' => $c };
            //::           my $n = $stair_end->{$dir}{'next'};
            //::              $end->{'next_row'} = $end->{'row'} + $n->[0];
            //::              $end->{'next_col'} = $end->{'col'} + $n->[1];
            //:: 
            //::           push(@list,$end); next COL;
            //::         }
            //::       }
            //::     }
            //::   }
            //::   return @list;
            //:: }
            */
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

                    foreach (Cardinal dir in stair_end2.Keys)
                    {
                        if (check_tunnel(dungeon.cell, (r, c), stair_end2[dir]))
                        {
                            StairEnd end = new() { row = r, col = c };
                            (int, int) n = stair_end2[dir].Next;
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
    }
}
#pragma warning restore IDE1006 // Naming Styles
