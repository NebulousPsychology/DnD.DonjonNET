// Adapted from https://donjon.bin.sh/code/dungeon/dungeon.pl
// https://creativecommons.org/licenses/by-nc/3.0/

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;


namespace Donjon.ImageTools;
/// <remarks>
/// https://www.hanselman.com/blog/how-do-you-use-systemdrawing-in-net-core
/// https://docs.sixlabors.com/articles/imagesharp/?tabs=tabid-1
/// https://docs.sixlabors.com/api/ImageSharp/SixLabors.ImageSharp.Processing.Processors.Transforms.AutoOrientProcessor.html
/// </remarks>
public class DungeonImageMap(ILogger<DungeonImageMap> log, IOptions<ImageMapOptions> options)
{

    public void DrawDoors(Image map, Dungeon d, bool showSecrets)
    {
        using Image<Rgba32> doorStamp = Image.Load<Rgba32>(options.Value.DoorPath);
        map.Mutate(ctxt => AddDoors(ctxt, d.door, doorStamp));

        void AddDoors(IImageProcessingContext context, IEnumerable<DoorData> doors, Image stamp)
        {
            Size tileSize = new(options.Value.PixelsPerTile);

            foreach (var d in doors)
            {
                char dir = d.open_dir?.FirstOrDefault() ?? 'e';
                RotateMode orientation = dir switch
                {
                    'e' => RotateMode.None,
                    's' => RotateMode.Rotate90,
                    'w' => RotateMode.Rotate180,
                    'n' => RotateMode.Rotate270,
                    _ => RotateMode.None,
                };
                DrawFromCell(map, [d.Coord],
                 stampSelector: _ => doorStamp,// rc => doors.Single(door => door.row == rc.row && door.col == rc.col).key switch { },
                 null, additionalMutation: (mut, rc) => mut.Rotate(orientation)); //? how to write text?
            }
        }
    }

    public void DrawGrid(Image map, Image cell, Dungeon d)
    {
        DrawFromCell(map, GetCells(d).Where(c => d.cell[c.r, c.c] != Cellbits.NOTHING && !d.cell[c.r, c.c].HasAnyFlag(Cellbits.PERIMETER)),
         stampSelector: _ => cell, null,
        //  additionalMutation: (ctxt, coord) => d.cell[coord.row, coord.col].HasAnyFlag(Cellbits.ROOM) ? ctxt.Invert() : ctxt
         additionalMutation: (ctxt, coord) => d.cell[coord.row, coord.col] switch
         {
             Cellbits c when c.HasAnyFlag(Cellbits.DOOR_TRAPPED) => ctxt.BackgroundColor(Color.Magenta),
             Cellbits c when c.HasAnyFlag(Cellbits.DOOR_SECRET) => ctxt.BackgroundColor(Color.Cyan),
             Cellbits c when c.HasAnyFlag(Cellbits.STAIRS) => ctxt.BackgroundColor(Color.SeaGreen),
             Cellbits c when c.HasAnyFlag(Cellbits.ROOM) => ctxt.BackgroundColor(Color.AntiqueWhite),
             Cellbits c when c.HasAnyFlag(Cellbits.PERIMETER) => ctxt.BackgroundColor(Color.DarkGray).Invert(),
             Cellbits c when c.HasAnyFlag(Cellbits.CORRIDOR) => ctxt.BackgroundColor(Color.Firebrick),
             _ => ctxt.Invert(),
         }
         );
    }

    private static IEnumerable<(int r, int c)> GetCells(Dungeon d)
    {
        return Enumerable.Range(0, d.max_row)
                    .SelectMany(r => Enumerable.Range(0, d.max_col).Select(c => (r, c)));
    }

    /// <summary> 
    /// </summary>
    /// <param name="map"></param>
    /// <param name="cells"></param>
    /// <param name="stampSelector"></param>
    /// <param name="positionPerturb"> 
    ///     item1: shift position from center of cell
    ///     item2: shift size relative to cell size
    ///     item3: redefine source rectangle
    /// </param>
    /// <param name="additionalMutation">
    /// callback for any additional mutation to occur after the resize supplied by <paramref name="positionPerturb"/>
    /// </param>
    /// <returns></returns>
    /// <remarks>
    /// TODO: this is DRY gone wrong
    /// </remarks>
    void DrawFromCell(Image map, IEnumerable<(int row, int col)> cells,
        Func<(int row, int col), Image> stampSelector,
        Func<(int row, int col), (Size deltaPos, Size deltaCellSize, Rectangle? rectangle)>? positionPerturb = null,
        Func<IImageProcessingContext, (int row, int col), IImageProcessingContext>? additionalMutation = null
        )
    {
        static double SizeMagnitudeSquared(Size s) => s.Height * s.Height + s.Width * s.Width;
        Size tileSize = new(options.Value.PixelsPerTile);
        var tileSizeMagSq = SizeMagnitudeSquared(tileSize);
        // optional callbacks fall back to no-op
        additionalMutation ??= (IImageProcessingContext ctxt, (int, int) cell) => ctxt;
        positionPerturb ??= ((int, int) _) => (Size.Empty, Size.Empty, null);

        foreach (var c in cells)
        {
            var (deltaPos, deltaCellSize, rectangle) = positionPerturb(c);
            if (tileSizeMagSq < SizeMagnitudeSquared(deltaPos))
            {
                log.LogWarning("perturbing {psz}, which exceeds cell {cellsz}", deltaPos, tileSize);
            }

            Point mapgrid = options.Value.PixelsPerTile * new Point(c.col, y: c.row);
            Point mapgridcenter = mapgrid + tileSize / 2;

            // Mutation
            using Image rotatedstamp = stampSelector(c)
                .Clone(s => additionalMutation(s.Resize(tileSize + deltaCellSize), c));

            map.Mutate(context => context.DrawImage(
                foreground: rotatedstamp,
                backgroundLocation: mapgridcenter - (rotatedstamp.Bounds.Size / 2) + deltaPos,
                foregroundRectangle: rectangle ?? rotatedstamp.Bounds,
                //! could be an issue providing a rectangle, when the real-bounds will have been altered during Mutation
                opacity: 1f));
        }
    }

    public void CreateMap(Dungeon dungeon)
    {
        using var _ = log.BeginScope(nameof(CreateMap));
        log.LogInformation("Creating Map");
        //
        using Image<Rgba32> mapImage = new(
            width: dungeon.n_cols * options.Value.PixelsPerTile,
            height: dungeon.n_rows * options.Value.PixelsPerTile,
            backgroundColor: Color.CornflowerBlue);

        using Image stamp = Image.Load<Rgba32>(options.Value.CellTilePath);
        DrawGrid(mapImage, stamp, dungeon);

        DrawDoors(mapImage, dungeon, showSecrets: true);
        var filename = Path.GetFullPath($"test_{nameof(CreateMap)}.jpg");
        log.LogInformation("image at {file}", filename);
        mapImage.Save(filename); // Automatic encoder selected based on extension.
    }
}
