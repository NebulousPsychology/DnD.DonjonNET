using System.Diagnostics.CodeAnalysis;

using Donjon.Original;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Donjon;
#pragma warning disable IDE1006 // Naming Styles

public partial class DungeonGenRefactored
{
    public class InitCellsStep(
        ILogger<InitCellsStep> logger,
        IOptions<Settings> options
    ) : Pipeline.IPipelineOperation<IDungeon>
    {
        public bool TryInvoke(IDungeon input, [MaybeNullWhen(false), NotNullWhen(true)] out IDungeon? result)
        {
            result = this.init_cells(input);
            return true;
        }
        #region initialize cells

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dungeon"></param>
        /// <returns></returns>
        IDungeon init_cells(IDungeon dungeon)
        {
            /*
            //:: sub init_cells {
            //::   my ($dungeon) = @_;
            //::
            //::   my $r; for ($r = 0; $r <= $dungeon->{'n_rows'}; $r++) {
            //::     my $c; for ($c = 0; $c <= $dungeon->{'n_cols'}; $c++) {
            //::       $dungeon->{'cell'}[$r][$c] = $NOTHING;
            //::     }
            //::   }
            //::   srand($dungeon->{'seed'} + 0);
            //:: 
            //::   my $mask; if ($mask = $dungeon_layout->{$dungeon->{'dungeon_layout'}}) {
            //::     $dungeon = &mask_cells($dungeon,$mask);
            //::   } elsif ($dungeon->{'dungeon_layout'} eq 'Round') {
            //::     $dungeon = &round_mask($dungeon);
            //::   }
            //::   return $dungeon;
            //:: }
            */
            using (logger.BeginScope(nameof(init_cells)))
            {
                foreach (Coord rc in Dim2d.RangeUpperExclusive(0, 0, options.Value.Dungeon.n_rows, options.Value.Dungeon.n_cols))
                {
                    dungeon.cell.Set(rc, Cellbits.NOTHING);
                }

                if (TryGetDungeon_Layout_Mask(options.Value.Dungeon.dungeon_layout, out var mask))
                {
                    dungeon = mask_cells(dungeon, mask);
                }
                else if (options.Value.Dungeon.dungeon_layout.Equals("Round"))
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
        IDungeon mask_cells(IDungeon dungeon, int[,] mask)
        {
            /*
            //:: sub mask_cells {
            //::   my ($dungeon,$mask) = @_;
            //::   my $r_x = (scalar @{ $mask } * 1.0 / ($dungeon->{'n_rows'} + 1));
            //::   my $c_x = (scalar @{ $mask->[0] } * 1.0 / ($dungeon->{'n_cols'} + 1));
            //::   my $cell = $dungeon->{'cell'};
            //:: 
            //::   my $r; for ($r = 0; $r <= $dungeon->{'n_rows'}; $r++) {
            //::     my $c; for ($c = 0; $c <= $dungeon->{'n_cols'}; $c++) {
            //::       $cell->[$r][$c] = $BLOCKED unless ($mask->[$r * $r_x][$c * $c_x]);
            //::     }
            //::   }
            //::   return $dungeon;
            //:: }
            */
            using (logger.BeginScope(nameof(mask_cells)))
            {
                //* scale the mask coordinates up to dungeon dimensions
                var r_x = mask.GetLength(0) * 1 / (options.Value.Dungeon.n_rows + 1); //::   my $r_x = (scalar @{ $mask } * 1.0 / ($dungeon->{'n_rows'} + 1));
                var c_x = mask.GetLength(1) * 1 / (options.Value.Dungeon.n_cols + 1); //::   my $c_x = (scalar @{ $mask->[0] } * 1.0 / ($dungeon->{'n_cols'} + 1));

                //RASTER: REALSPACE: INCLUSIVE <0,0>..<nrows,ncols>
                foreach (var (r, c) in Dim2d.RangeInclusive(0, options.Value.Dungeon.n_rows, 0, options.Value.Dungeon.n_cols))
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
        IDungeon round_mask(IDungeon dungeon)
        {
            /*
            //:: sub round_mask {
            //::   my ($dungeon) = @_;
            //::   my $center_r = int($dungeon->{'n_rows'} / 2);
            //::   my $center_c = int($dungeon->{'n_cols'} / 2);
            //::   my $cell = $dungeon->{'cell'};
            //:: 
            //::   my $r; for ($r = 0; $r <= $dungeon->{'n_rows'}; $r++) {
            //::     my $c; for ($c = 0; $c <= $dungeon->{'n_cols'}; $c++) {
            //::       my $d = sqrt((($r - $center_r) ** 2) + (($c - $center_c) ** 2));
            //::       $cell->[$r][$c] = $BLOCKED if ($d > $center_c);
            //::     }
            //::   }
            //::   return $dungeon;
            //:: }
            */
            using (logger.BeginScope(nameof(round_mask)))
            {
                var centerOfMap = (r: options.Value.Dungeon.n_rows / 2, c: options.Value.Dungeon.n_cols / 2);

                //RASTER: REALSPACE: INCLUSIVE <0,0>..<nrows,ncols>
                foreach (var (r, c) in Dim2d.RangeInclusive(0, options.Value.Dungeon.n_rows, 0, options.Value.Dungeon.n_cols))
                {
                    var radius = Math.Sqrt(Math.Pow(r - centerOfMap.r, 2) + Math.Pow(c - centerOfMap.c, 2));
                    //::       $cell->[$r][$c] = $BLOCKED if ($d > $center_c);
                    dungeon.cell[r, c] = (radius > centerOfMap.c) ? Cellbits.BLOCKED : dungeon.cell[r, c];
                }
                return dungeon;
            }
        }
        #endregion initialize cells
    }
}
#pragma warning restore IDE1006 // Naming Styles
