using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Options;

namespace Donjon.ImageTools;
public class ImageMapOptions
{
    public required string BackgroundFileName { get; set; } = "";
    public required string CellTilePath { get; set; }
    public required string DoorPath { get; set; }

    [Range(1, double.MaxValue)]
    public int PixelsPerTile { get; set; } = 70;
}