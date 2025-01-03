
using Donjon.Original;

using Microsoft.Extensions.Logging;

using SixLabors.ImageSharp;

namespace Donjon;
public partial class DungeonGenRefactored
{
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

            //RASTER: HEMI: INCLUSIVE INSET <1,1>..<ni=nrows/2-1 (odd),nj=ncols/2-1 (odd)>
            // from 1,1 to n_i-1,n_j-1, inclusive
            foreach (var (i, j) in Dim2d.RangeInclusive(1, dungeon.n_i - 1, 1, dungeon.n_j - 1).Cast<(Hemispace<int>, Hemispace<int>)>())
            {
                var (r, c) = (i, j).ToRealspace();
                if (dungeon.cell[r, c].HasAnyFlag(Cellbits.CORRIDOR)) // if we see Corridor, we already tunneled at [r,c]
                {
                    logger.LogDebug(1, "Consider tunnling from ({r},{c})... reject (already is corridor)", r, c);
                    continue;
                }
                else
                {
                    logger.LogDebug(2, "About to tunnel from ({r},{c}) because it isn't CORRIDOR", r, c);
                }

                // ? but then we snap back into index-space?
                dungeon = tunnel(dungeon, i.Value, j.Value);
                logger.LogInformation(3, "Finished a Tunnel from {fn}! \nnew map:{map}",
                    nameof(corridors), DescribeDungeonLite(dungeon));
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
        //RASTER: i,j from HEMI: INCLUSIVE INSET <1,1>..<ni=nrows/2-1 (odd),nj=ncols/2-1 (odd)>
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
    sealed class DirectionHelper
    {
        public static (int, int) Next(Cardinal d, (int, int) from) => (0, 0);
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
                    // pUncurviness is the percent chance that we WON'T turn, and will continue in the last direction
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
        //RASTER: i,j from HEMI: INCLUSIVE INSET <1,1>..<ni=nrows/2-1 (odd),nj=ncols/2-1 (odd)>
        //:: using (logger.BeginScope(nameof(open_tunnel)))
        //:: {
        // find the current rowspace coordinate
        // r and c will be odd
        (int r, int c) curr = (i * 2 + 1, j * 2 + 1);
        // find the next rowspace coordinate in the proposed direction
        (int r, int c) next = ((i + di[dir]) * 2 + 1, (j + dj[dir]) * 2 + 1);
        // two steps in indicated direction, +one,one
        // the rowspace coordinate between them (because it's a single odd-row increment, the middle should be on the even)
        (int r, int c) mid = ((curr.r + next.r) / 2, (curr.c + next.c) / 2); // it looks like a tohemi, but it's a midpoint

        return sound_tunnel(dungeon, mid.r, mid.c, next.r, next.c) && delve_tunnel(dungeon, curr.r, curr.c, next.r, next.c);
        //:: } // /scope
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

            //RASTER: REAL: FILTER <nr,nc> in [<0,0>..<nrows,ncols>)
            if (next_r < 0 || next_r >= dungeon.n_rows) return false;
            if (next_c < 0 || next_c >= dungeon.n_cols) return false;

            // find extents in an order that is convenient for iteration
            Realspace<int> r1 = Math.Min(mid_r, next_r);
            Realspace<int> c1 = Math.Min(mid_c, next_c);
            Realspace<int> r2 = Math.Max(mid_r, next_r);
            Realspace<int> c2 = Math.Max(mid_c, next_c);

            //RASTER:new REAL: INCLUSIVE <r1,c1>..<r2,c2>
            foreach (var (r, c) in Dim2d.RangeInclusive(r1, r2, c1, c2))
            {
                // HasFlag requires the FULL bitmask, not ANY part of it ( x&y !=0 )
                if (dungeon.cell[r, c].HasAnyFlag(Cellbits.BLOCK_CORR))
                {
                    logger.LogDebug("tunnel sounding false from {r},{c} to {x},{y}: a block_corr was found", mid_r, mid_c, next_r, next_c);
                    return false;
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

            //RASTER: REAL: INCLUSIVE <r1,c1>..<r2,c2>
            foreach (var (r, c) in Dim2d.RangeInclusive(r1, r2, c1, c2))
            {
                logger.LogTrace("delve tunnel: mark {r},{c} as non-entrance corridor", r, c);
                dungeon.cell[r, c] &= ~Cellbits.ENTRANCE; // filter to everybit EXCEPT entrance (erase Entrance bits)
                dungeon.cell[r, c] |= Cellbits.CORRIDOR; // set Corridor
            }
            return true;
        }
    }
    #endregion Corridors

}