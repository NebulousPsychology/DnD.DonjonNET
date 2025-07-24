using System.Diagnostics.CodeAnalysis;

using Donjon.Original;
using Donjon.Pipeline;

using Microsoft.Extensions.Logging;

namespace Donjon;
#pragma warning disable IDE1006 // Naming Styles

public partial class DungeonGenRefactored
{
    /// <summary>
    /// A pipeline operation that can <see cref="check_tunnel(Cellbits[,], Coord, IPrunableSubject)"/>
    /// </summary>
    /// <param name="logger"></param>
    public abstract class PruningStepBase(ILogger<PruningStepBase> logger) : IPipelineOperation<IDungeon>
    {
        public abstract bool TryInvoke(IDungeon input, [MaybeNullWhen(false), NotNullWhen(true)] out IDungeon? result);

        /// <summary>
        /// check prunability in <see cref="collapse(IDungeon, Coord, Dictionary{Cardinal, CloseWallingArguments})"/> for <see cref="clean_dungeon(IDungeon)"/>
        /// or <seealso cref="stair_ends(IDungeon)"/> for <see cref="emplace_stairs(IDungeon)"/>:
        ///  stairwalls are only deletable if the corridor cells are marked as CORRIDOR.
        /// In either usage, cell rc is deletable if ALL wallcells ARE NOT open
        /// </summary> 
        /// <param name="cell"></param>
        /// <param name="r">anchor row within the map, points in <see cref="check"/> are relative to this</param>
        /// <param name="c">anchor col within the map, points in <see cref="check"/> are relative to this</param>
        /// <param name="check"></param>
        /// <returns>true if deletion should occur</returns>
        protected bool check_tunnel(Cellbits[,] cell, Coord rc, IPrunableSubject check)
        {
            /*
            //:: sub check_tunnel {
            //::   my ($cell,$r,$c,$check) = @_;
            //::   my $list;
            //::
            //::   if ($list = $check->{'corridor'}) {
            //::     my $p; foreach $p (@{ $list }) {
            //::       return 0 unless ($cell->[$r+$p->[0]][$c+$p->[1]] == $CORRIDOR);
            //::     }
            //::   }
            //::   if ($list = $check->{'walled'}) {
            //::     my $p; foreach $p (@{ $list }) {
            //::       return 0 if ($cell->[$r+$p->[0]][$c+$p->[1]] & $OPENSPACE);
            //::     }
            //::   }
            //::   return 1;
            //:: }
            */
            using (logger.BeginScope(nameof(check_tunnel)))
            {
                // only a full corridor is deletable
                if (check is StairWallingArguments { Corridor: IEnumerable<Coord> corridor_list })//.TryGetValue("corridor", out var list))
                {
                    logger.LogTrace(1, "test all {n} listed cells relative to ({rc}) are Corridor", corridor_list.Count(), rc);

                    // for stairwalls  undeletable(false)  if ANY corridor cell IS NOT marked as CORRIDOR
                    if (corridor_list.Any(p => cell.Get(rc + p) is not Cellbits.CORRIDOR))
                        return false;
                }

                //? what does it mean to be a cell in walledList?
                // checks as deletable if ALL wallcell IS NOT open
                return check.Walled.All(p => cell.Get(rc + p).HasAnyFlag(Cellbits.OPENSPACE | Cellbits.ENTRANCE) is false); //! added Entrance 
            } // /logscope
        }
    }
}
#pragma warning restore IDE1006 // Naming Styles
