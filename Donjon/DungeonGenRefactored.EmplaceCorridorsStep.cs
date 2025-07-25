
using System.Diagnostics.CodeAnalysis;

using Donjon.Original;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SixLabors.ImageSharp;

namespace Donjon;

public partial class DungeonGenRefactored
{
    public class EmplaceCorridorsStep(
        ILogger<EmplaceCorridorsStep> logger,
        IOptions<Settings> settings,
        Random random
        )
    : Pipeline.IPipelineOperation<IDungeon>
    {
        public bool TryInvoke(IDungeon input, [MaybeNullWhen(false), NotNullWhen(true)] out IDungeon? result)
        {
            try
            {
                result = corridors(input);
            }
            catch
            {
                result = null;
            }
            return result is not null;
        }
        #region Corridors

        /// <summary>
        /// # generate corridors
        /// </summary>
        /// <param name="dungeon"></param>
        /// <returns></returns>
        IDungeon corridors(IDungeon dungeon)
        {
            /*
            //:: sub corridors {
            //::   my ($dungeon) = @_;
            //::   my $cell = $dungeon->{'cell'};
            //::
            //::   my $i; for ($i = 1; $i < $dungeon->{'n_i'}; $i++) {
            //::       my $r = ($i * 2) + 1;
            //::     my $j; for ($j = 1; $j < $dungeon->{'n_j'}; $j++) {
            //::       my $c = ($j * 2) + 1;
            //::
            //::       next if ($cell->[$r][$c] & $CORRIDOR);
            //::       $dungeon = &tunnel($dungeon,$i,$j);
            //::     }
            //::   }
            //::   return $dungeon;
            //:: }
            */
            using (logger.BeginScope(nameof(corridors)))
            {
                //! For Odd-numbered rows and columns, starting at 3,3.  
                //! To this end, ij-indexspace is halfgrid, scaled and offset to return to rowspace

                //RASTER: HEMI: INCLUSIVE INSET <1,1>..<ni=nrows/2-1 (odd),nj=ncols/2-1 (odd)>
                // from 1,1 to n_i-1,n_j-1, inclusive
                foreach (Hemispace<Coord> ij in Dim2d
                    .RangeInclusive(1, dungeon.n_i - 1, 1, dungeon.n_j - 1).AsHemi())
                {
                    var (r, c) = ij.ToRealspace();
                    if (dungeon.cell[r, c].HasAnyFlag(Cellbits.CORRIDOR)) // if we see Corridor, we already tunneled at [r,c]
                    {
                        // logger.LogDebug(1, "Consider tunnling from ({r},{c})... reject (already is corridor)", r, c);
                        continue;
                    }
                    else
                    {
                        logger.LogDebug(2, "About to tunnel from ({r},{c}) because it isn't CORRIDOR", r, c);
                    }

                    // ? but then we snap back into index-space?
                    dungeon = tunnel(dungeon, ij);
                }
                return dungeon;
            }
        }

        /// <summary>
        ///  recursively tunnel, in odds-indexspace
        /// </summary>
        /// <param name="dungeon"></param>
        /// <param name="i">3+2n odd index space row</param>
        /// <param name="j">3+2n odd index space col</param>
        /// <param name="lastdir"></param>
        /// <returns></returns>
        IDungeon tunnel(IDungeon dungeon, Hemispace<Coord> ij, Cardinal? lastdir = null)
        {
            /*
            //:: sub tunnel {
            //::   my ($dungeon,$i,$j,$last_dir) = @_;
            //::   my @dirs = &tunnel_dirs($dungeon,$last_dir);
            //:: 
            //::   my $dir; foreach $dir (@dirs) {
            //::     if (&open_tunnel($dungeon,$i,$j,$dir)) {
            //::       my $next_i = $i + $di->{$dir};
            //::       my $next_j = $j + $dj->{$dir};
            //:: 
            //::       $dungeon = &tunnel($dungeon,$next_i,$next_j,$dir);
            //::     }
            //::   }
            //::   return $dungeon;
            //:: }
            */
            // using (logger.BeginScope(nameof(tunnel)))
            // {
            //RASTER: i,j from HEMI: INCLUSIVE INSET <1,1>..<ni=nrows/2-1 (odd),nj=ncols/2-1 (odd)>
            IEnumerable<Cardinal> dirs = tunnel_dirs(dungeon, lastdir);
            logger.LogTrace("Tunneling [{dirs}] from indexspace({ij})=rowspace({rc})",
                string.Join(",", dirs), ij, ij.ToRealspace());
            foreach (var dir in dirs)
            {
                if (open_tunnel(dungeon, ij, dir))
                {
                    //~~ (int next_i, int next_j) = (i + di[dir], j + dj[dir]);
                    Hemispace<Coord> next = ij.Add((Coord)dir);
                    dungeon = tunnel(dungeon, next, dir);
                }
            }
            return dungeon;
            // }
        }
        sealed class DirectionHelper
        {
            public static (int, int) Next(Cardinal d, (int, int) from) => (0, 0);
        }

        /// <summary>
        /// tunnel directions priority list, accounting for tendency to continue in the last direction (<see cref="corridor_layout"/>)
        /// </summary>
        /// <param name="dungeon"></param>
        /// <param name="last_dir"></param>
        /// <returns>A shuffled series of directions to consider turning</returns>
        IEnumerable<Cardinal> tunnel_dirs(IDungeon dungeon, Cardinal? last_dir)
        {
            /*
            //:: sub tunnel_dirs {
            //::   my ($dungeon,$last_dir) = @_;
            //::   my $p = $corridor_layout->{$dungeon->{'corridor_layout'}};
            //::   my @dirs = &shuffle(@dj_dirs);
            //::
            //::   if ($last_dir && $p) {
            //::     unshift(@dirs,$last_dir) if (int(rand(100)) < $p);
            //::   }
            //::   return @dirs;
            //:: }
            */
            using (logger.BeginScope(nameof(tunnel_dirs)))
            {
                Cardinal[] dirs = [.. DungeonGen.dj_dirs];
                random.Shuffle(dirs); // direction keys, but in a random order

                if (last_dir is not null
                    && TryGetCorridorUncurviness(settings.Value.Corridors.corridor_layout, out int? pUncurviness))
                {
                    // pUncurviness is the percent chance that we WON'T turn, and will continue in the last direction
                    if (pUncurviness > 0 && random.Next(100) < pUncurviness)
                    {
                        // prepend lastdir to dirs, so we'll address all the directions, but continue an existing direction first
                        return Enumerable.Concat([last_dir.Value], dirs);
                    }
                }
                return dirs.AsEnumerable();
            }
        }

        /// <summary>
        ///  open tunnel
        /// </summary>
        /// <param name="dungeon"></param>
        /// <param name="i">oddrow indexspace</param>
        /// <param name="j">oddcol indexspace</param>
        /// <param name="dir"></param>
        /// <returns>true if the <see cref="delve_tunnel"/> occurred</returns>
        bool open_tunnel(IDungeon dungeon, Hemispace<Coord> ij, Cardinal dir)
        {
            /*
            //:: sub open_tunnel {
            //::   my ($dungeon,$i,$j,$dir) = @_;
            //::
            //::   my $this_r = ($i * 2) + 1;
            //::   my $this_c = ($j * 2) + 1;
            //::   my $next_r = (($i + $di->{$dir}) * 2) + 1;
            //::   my $next_c = (($j + $dj->{$dir}) * 2) + 1;
            //::   my $mid_r = ($this_r + $next_r) / 2;
            //::   my $mid_c = ($this_c + $next_c) / 2;
            //::
            //::   if (&sound_tunnel($dungeon,$mid_r,$mid_c,$next_r,$next_c)) {
            //::     return &delve_tunnel($dungeon,$this_r,$this_c,$next_r,$next_c);
            //::   } else {
            //::     return 0;
            //::   }
            //:: }
            */
            //RASTER: i,j from HEMI: INCLUSIVE INSET <1,1>..<ni=nrows/2-1 (odd),nj=ncols/2-1 (odd)>
            using (logger.BeginScope(nameof(open_tunnel)))
            {
                // find the current rowspace coordinate
                //~~ // r and c will be odd
                //~~ (int r, int c) curr = (i * 2 + 1, j * 2 + 1);
                Realspace<Coord> curr = ij.ToRealspace();
                // find the next rowspace coordinate in the proposed direction
                // ~~(int r, int c) next = ((i + di[dir]) * 2 + 1, (j + dj[dir]) * 2 + 1);
                Realspace<Coord> next = ij.Add((Coord)dir).ToRealspace();
                // two steps in indicated direction, +one,one
                // the rowspace coordinate between them (because it's a single odd-row increment, the middle should be on the even)
                //~~ (int r, int c) mid = ((curr.r + next.r) / 2, (curr.c + next.c) / 2); // it looks like a tohemi, but it's a midpoint
                Realspace<Coord> mid = curr.Add(next).Value / 2; // it only looks like a tohemi, but it's a midpoint

                return sound_tunnel(dungeon, mid, next) && delve_tunnel(dungeon, curr, next);
            } // /scope
        }

        /// <summary>
        /// sound tunnel
        /// to keep from opening blocked cells, room perimeters, or other corridors
        /// </summary>
        /// <param name="dungeon"></param>
        /// <param name="mid_r"></param>
        /// <param name="mid_c"></param>
        /// <param name="next_r"></param>
        /// <param name="next_c"></param>
        /// <returns>
        /// true if no cell in the proposed tunnel is
        /// <see cref="Cellbits.BLOCK_CORR"/> (Blocked/Perimeter/Corridor) </returns>
        bool sound_tunnel(IDungeon dungeon, Realspace<Coord> mid, Realspace<Coord> next)
        {
            /*
            //:: sub sound_tunnel {
            //::   my ($dungeon,$mid_r,$mid_c,$next_r,$next_c) = @_;
            //::      return 0 if ($next_r < 0 || $next_r > $dungeon->{'n_rows'});
            //::      return 0 if ($next_c < 0 || $next_c > $dungeon->{'n_cols'});
            //::   my $cell = $dungeon->{'cell'};
            //::   my ($r1,$r2) = sort { $a <=> $b } ($mid_r,$next_r);
            //::   my ($c1,$c2) = sort { $a <=> $b } ($mid_c,$next_c);
            //::
            //::   my $r; for ($r = $r1; $r <= $r2; $r++) {
            //::     my $c; for ($c = $c1; $c <= $c2; $c++) {
            //::       return 0 if ($cell->[$r][$c] & $BLOCK_CORR);
            //::     }
            //::   }
            //::   return 1;
            //:: }
            */
            using (logger.BeginScope(nameof(sound_tunnel)))
            {
                // reject the proposed tunnel if the destination cell is out of bounds

                //RASTER: REAL: FILTER <nr,nc> in [<0,0>..<nrows,ncols>)
                //~~ if (next.r < 0 || next.r >= settings.Value.Dungeon.n_rows) return false;
                //~~ if (next.c < 0 || next.c >= settings.Value.Dungeon.n_cols) return false;
                if (settings.Value.Dungeon.Contains(next) is false) return false;

                // find extents in an order that is convenient for iteration
                //~~ Realspace<int> r1 = Math.Min(mid.Value.r, next.r);
                //~~ Realspace<int> c1 = Math.Min(mid.Value.c, next.c);
                //~~ Realspace<int> r2 = Math.Max(mid.Value.r, next.r);
                //~~ Realspace<int> c2 = Math.Max(mid.Value.c, next.c);
                var rc1 = Realspace<Coord>.Min(mid, next);
                var rc2 = Realspace<Coord>.Max(mid, next);

                //RASTER:new REAL: INCLUSIVE <r1,c1>..<r2,c2>
                //~~ foreach (var (r, c) in Dim2d.RangeInclusive(r1, r2, c1, c2))
                //~~ {
                //~~     // HasFlag requires the FULL bitmask, not ANY part of it ( x&y !=0 )
                //~~     if (dungeon.cell[r, c].HasAnyFlag(Cellbits.BLOCK_CORR))
                //~~     {
                //~~         // logger.LogDebug("tunnel sounding false from {r},{c} to {x},{y}: a block_corr was found", mid_r, mid_c, next_r, next_c);
                //~~         return false;
                //~~     }
                //~~ }
                //~~ // logger.LogDebug("tunnel sounding true from {r},{c} to {x},{y}", mid_r, mid_c, next_r, next_c);
                //~~ return true;
                var is_unblocked = Dim2d.RangeInclusive(rc1, rc2)
                                        .All(cell => dungeon.cell.Get(cell).HasAnyFlag(Cellbits.BLOCK_CORR) is false);
                logger.LogDebug("tunnel is unblocked from {rc} to {xy} (no BLOCK_CORR) == {ans}", mid, next, is_unblocked);
                return is_unblocked;
            }
        }

        /// <summary>
        /// mark all cells in the given inclusive rectangle as <see cref="Cellbits.CORRIDOR"/>, non-<see cref="Cellbits.ENTRANCE"/>
        /// </summary>
        /// <remarks><code>
        /// </code></remarks>
        /// <param name="dungeon"></param>
        /// <param name="this_r"></param>
        /// <param name="this_c"></param>
        /// <param name="next_r"></param>
        /// <param name="next_c"></param>
        /// <returns>true, always</returns>
        bool delve_tunnel(IDungeon dungeon, Realspace<Coord> @this, Realspace<Coord> next)
        {
            /*
            //:: sub delve_tunnel {
            //::   my ($dungeon,$this_r,$this_c,$next_r,$next_c) = @_;
            //::   my $cell = $dungeon->{'cell'};
            //::   my ($r1,$r2) = sort { $a <=> $b } ($this_r,$next_r);
            //::   my ($c1,$c2) = sort { $a <=> $b } ($this_c,$next_c);
            //:: 
            //::   my $r; for ($r = $r1; $r <= $r2; $r++) {
            //::     my $c; for ($c = $c1; $c <= $c2; $c++) {
            //::       $cell->[$r][$c] &= ~ $ENTRANCE;
            //::       $cell->[$r][$c] |= $CORRIDOR;
            //::     }
            //::   }
            //::   return 1;
            //:: }
            */
            using (logger.BeginScope("{fn}: delve from ({rc}) to ({next})",
                nameof(delve_tunnel), @this, next))
            {
                var rc1 = Realspace<Coord>.Min(@this, next);
                var rc2 = Realspace<Coord>.Max(@this, next);

                //RASTER: REAL: INCLUSIVE <r1,c1>..<r2,c2>
                foreach (var coord in Dim2d.RangeInclusive(rc1, rc2))
                {
                    logger.LogTrace("delve tunnel: mark {rc} as non-entrance corridor", coord);
                    // filter to everybit EXCEPT entrance (erase Entrance bits), and set Corridor
                    dungeon.cell.Set(coord, v => (v & ~Cellbits.ENTRANCE) | Cellbits.CORRIDOR);
                }
                return true;
            }
        }
        #endregion Corridors
    }
}