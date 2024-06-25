namespace Donjon.Test;

using Donjon.ImageTools;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;
using NSubstitute.Extensions;

using Xunit.Abstractions;
public class ImageTest(ITestOutputHelper outputHelper)
{
    readonly XunitLogger<ImageTest> _xunitLogger = new(outputHelper, LogLevel.Information);
    [Fact]
    public void TestName()
    {
        // Given
        var opts = Substitute.For<IOptions<ImageMapOptions>>();
        opts.Value.Returns(new ImageMapOptions
        {
            BackgroundFileName = "Images/cell_100x100.png",
            CellTilePath = "Images/cell_100x100.png",
            DoorPath = "Images/door_70x70.png",
            PixelsPerTile = 70,
        });

        Assert.True(File.Exists(opts.Value.BackgroundFileName), userMessage: $"fnf: {opts.Value.BackgroundFileName}");
        Assert.True(File.Exists(opts.Value.CellTilePath), userMessage: $"fnf: {opts.Value.CellTilePath}");
        Assert.True(File.Exists(opts.Value.DoorPath), userMessage: $"fnf: {opts.Value.DoorPath}");
        // When
        var g = new DungeonGen(new XunitLogger<DungeonGen>(outputHelper, LogLevel.Information));
        var dungeon = g.Create_dungeon(new Dungeon { seed = 12345 });

        var i = new DungeonImageMap(new XunitLogger<DungeonImageMap>(outputHelper, LogLevel.Trace), opts);
        i.CreateMap(dungeon);

        // Then
        Assert.Fail("for logs");
    }
}