// Adapted from https://donjon.bin.sh/code/dungeon/dungeon.pl
// https://creativecommons.org/licenses/by-nc/3.0/

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
// would need SixLabors.Fonts 

namespace Donjon.ImageTools;

/// <remarks>
/// https://www.hanselman.com/blog/how-do-you-use-systemdrawing-in-net-core
/// https://docs.sixlabors.com/articles/imagesharp/?tabs=tabid-1
/// https://docs.sixlabors.com/api/ImageSharp/SixLabors.ImageSharp.Processing.Processors.Transforms.AutoOrientProcessor.html
/// </remarks>
public class DungeonImageMapBuilder(ILogger<DungeonImageMapBuilder> log, IOptions<ImageMapOptions> imgSettings)
{

    public void DrawDoors(Image map, Dungeon d, bool showSecrets)
    {
        using Image<Rgba32> doorStamp = Image.Load<Rgba32>(imgSettings.Value.DoorPath);
        using Image<Rgba32> doorStampArch = Image.Load<Rgba32>(imgSettings.Value.DoorPathArch);
        using Image<Rgba32> doorStampPortc = Image.Load<Rgba32>(imgSettings.Value.DoorPathPortc);
        using Image<Rgba32> doorStampSecret = Image.Load<Rgba32>(imgSettings.Value.DoorPathSecret);
        using Image<Rgba32> doorStampTrap = Image.Load<Rgba32>(imgSettings.Value.DoorPathTrap);
        Image doorStampSelector(DoorData door) => door.key switch
        {//  arch, open, lock, trap, secret, portc
            var s when s is "portc" => doorStampPortc,
            var s when s is "arch" => doorStampArch,
            var s when s is "lock" => doorStamp,
            var s when showSecrets && s is "trap" => doorStampTrap,
            var s when showSecrets && s is "secret" => doorStampSecret,
            _ => doorStamp
        };

        map.Mutate(ctxt => AddDoors(ctxt, d.door, doorStampSelector));

        void AddDoors(IImageProcessingContext context, IEnumerable<DoorData> doors, Func<DoorData, Image> stampSelector)
        {
            Size tileSize = new(imgSettings.Value.PixelsPerTile);

            foreach (var d in doors)
            {
                RotateMode orientation = d.open_dir switch
                {
                    Cardinal.east => RotateMode.None,
                    Cardinal.south => RotateMode.Rotate90,
                    Cardinal.west => RotateMode.Rotate180,
                    Cardinal.north => RotateMode.Rotate270,
                    _ => RotateMode.None,
                };
                StampEachCell(map, [d.Coord],
                 stampSelector: addr => doorStampSelector(doors.Single(door => door.row == addr.row && door.col == addr.col)), positionPerturb: null,
                 additionalMutation: (mut, rc) => mut.Rotate(orientation)); //? how to write text?
            }
        }
    }

    public void DrawGrid(Image map, Dungeon d)
    {
        using Image<Rgba32> cstamp = Image.Load<Rgba32>(imgSettings.Value.CellTilePath);
        using Image<Rgba32> bkstamp = Image.Load<Rgba32>(imgSettings.Value.BackgroundFileName);
        StampEachCell(
            map: map,
            cells: GetCells(d).Where(c => d.cell[c.r, c.c] != Cellbits.NOTHING && !d.cell[c.r, c.c].HasAnyFlag(Cellbits.PERIMETER)),
            stampSelector: coord => d.cell[coord.row, coord.col] switch
                {
                    Cellbits c when c.HasAnyFlag(Cellbits.ROOM) => cstamp,
                    Cellbits c when c.HasAnyFlag(Cellbits.CORRIDOR) => bkstamp,
                    _ => cstamp,
                },
            positionPerturb: null,
            //  additionalMutation: (ctxt, coord) => d.cell[coord.row, coord.col].HasAnyFlag(Cellbits.ROOM) ? ctxt.Invert() : ctxt
            additionalMutation: (ctxt, coord) => d.cell[coord.row, coord.col] switch
                {
                    Cellbits c when c.HasAnyFlag(Cellbits.DOOR_TRAPPED) => ctxt.BackgroundColor(Color.Magenta),
                    Cellbits c when c.HasAnyFlag(Cellbits.DOOR_SECRET) => ctxt.BackgroundColor(Color.Cyan),
                    Cellbits c when c.HasAnyFlag(Cellbits.STAIR_UP) => ctxt.BackgroundColor(Color.SeaGreen),
                    Cellbits c when c.HasAnyFlag(Cellbits.STAIR_DN) => ctxt.BackgroundColor(Color.Red),
                    Cellbits c when c.HasAnyFlag(Cellbits.PERIMETER) => ctxt.BackgroundColor(Color.Black),
                    Cellbits c when c.HasAnyFlag(Cellbits.ROOM) => ctxt.BackgroundColor(Color.LightGrey),//before corridor
                    Cellbits c when c.HasAnyFlag(Cellbits.CORRIDOR) => ctxt.BackgroundColor(Color.DarkGray),
                    _ => ctxt.Invert(),
                }
        );
    }

    /// <summary>
    /// FIXME: GetCells should be a feature of the dungeon interface
    /// </summary>
    /// <param name="r"></param>
    /// <param name="d"></param>
    private static IEnumerable<(int r, int c)> GetCells(Dungeon d)
        => Enumerable.Range(0, d.max_row).SelectMany(r => Enumerable.Range(0, d.max_col).Select(c => (r, c)));


    /// <summary>
    /// draw a stamp in each cell
    /// </summary>
    /// <param name="map">an image for drawing in</param>
    /// <param name="cells">a sequence of cells to draw stamps on</param>
    /// <param name="stampSelector">look up an image stamp from cell address</param>
    /// <param name="positionPerturb"> 
    ///     output item1: shift position from center of cell
    ///     output item2: shift size relative to cell size
    ///     output item3: redefine source rectangle
    /// </param>
    /// <param name="additionalMutation">
    /// callback for any additional mutation to occur after resize-to-cell supplied by <paramref name="positionPerturb"/>
    /// </param> 
    /// <remarks>
    /// TODO: this is DRY gone wrong
    /// </remarks>
    void StampEachCell(Image map, IEnumerable<(int row, int col)> cells,
        Func<(int row, int col), Image> stampSelector,
        Func<(int row, int col), (Size deltaPos, Size deltaCellSize, Rectangle? rectangle)>? positionPerturb = null,
        Func<IImageProcessingContext, (int row, int col), IImageProcessingContext>? additionalMutation = null
        )
    {
        static double SizeMagnitudeSquared(Size s) => s.Height * s.Height + s.Width * s.Width;
        // Optional mutators fallback to no-op
        additionalMutation ??= (IImageProcessingContext ctxt, (int, int) cell) => ctxt;
        positionPerturb ??= ((int, int) _) => (Size.Empty, Size.Empty, null);

        Size tileSize = new(imgSettings.Value.PixelsPerTile);
        var tileSizeMagSq = SizeMagnitudeSquared(tileSize);
        foreach (var c in cells)
        {
            var (deltaPos, deltaCellSize, rectangle) = positionPerturb(c);
            if (tileSizeMagSq < SizeMagnitudeSquared(deltaPos))
            {
                // moving the stamp further than the cell limits is odd
                log.LogWarning("perturbing {psz}, which exceeds cell {cellsz}", deltaPos, tileSize);
            }

            // Mutation
            using Image rotatedstamp = stampSelector(c)
                .Clone(s => additionalMutation(s.Resize(tileSize + deltaCellSize), c));

            // because stamp size waits until modifiers, find the corner to draw at
            Point cellCenter = imgSettings.Value.PixelsPerTile * new Point(c.col, y: c.row) // start from cell c's upper-left pixel
                + (tileSize / 2); // find cell c's centerpoint
            Point stampCorner = cellCenter - (rotatedstamp.Bounds.Size / 2); // find upper-left of stamp, when at cell c
            // render into position
            map.Mutate(context => context.DrawImage(
                foreground: rotatedstamp,
                backgroundLocation: stampCorner + deltaPos,
                foregroundRectangle: rectangle ?? rotatedstamp.Bounds,
                //! could be an issue providing a rectangle, when the real-bounds will have been altered during Mutation
                opacity: 1f));
        }
    }

    /// <summary>
    /// Render dungeon map to file
    /// </summary>
    /// <param name="dungeon"></param>
    public void CreateMap(Dungeon dungeon, string path, bool showSecrets = true)
    {
        using var _ = log.BeginScope(nameof(CreateMap));
        log.LogInformation("Creating Map");
        var filename = Path.GetFullPath(path);

        // prep output raster image
        using Image<Rgba32> mapImage = new(
            width: dungeon.n_cols * imgSettings.Value.PixelsPerTile,
            height: dungeon.n_rows * imgSettings.Value.PixelsPerTile,
            backgroundColor: Color.CornflowerBlue);
        // prep token stamps
        // using Image stamp = Image.Load<Rgba32>(imgSettings.Value.CellTilePath);

        // Draw the maze
        DrawGrid(mapImage, dungeon);
        DrawDoors(mapImage, dungeon, showSecrets);

        // save
        log.LogInformation("Save to {file}", filename);
        mapImage.Save(filename); // Automatic encoder selected based on extension.
    }
}
