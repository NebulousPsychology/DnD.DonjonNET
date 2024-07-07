using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Options;

namespace Donjon.ImageTools;
public class ImageMapOptions
{
    public required string BackgroundFileName { get; set; } = "";
    public required string CellTilePath { get; set; }
    public required string DoorPath { get; set; }
    public required string DoorPathArch { get; set; }
    public required string DoorPathPortc { get; set; }
    public required string DoorPathSecret { get; set; }
    public required string DoorPathTrap { get; set; }

    [Range(1, double.MaxValue)]
    public int PixelsPerTile { get; set; } = 70;
}