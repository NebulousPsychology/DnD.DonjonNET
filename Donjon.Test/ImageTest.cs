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
            DoorPathArch = "Images/door_arch_70x70.png",
            DoorPathPortc = "Images/door_portc_70x70.png",
            DoorPathSecret = "Images/door_secret_70x70.png",
            DoorPathTrap = "Images/door_trapped_70x70.png",
            PixelsPerTile = 70,
        });
        foreach (var pair in new Dictionary<string, string>
        {
            [nameof(opts.Value.BackgroundFileName)] = opts.Value.BackgroundFileName,
            [nameof(opts.Value.CellTilePath)] = opts.Value.CellTilePath,
            [nameof(opts.Value.DoorPath)] = opts.Value.DoorPath,
            [nameof(opts.Value.DoorPathArch)] = opts.Value.DoorPathArch,
            [nameof(opts.Value.DoorPathPortc)] = opts.Value.DoorPathPortc,
            [nameof(opts.Value.DoorPathSecret)] = opts.Value.DoorPathSecret,
            [nameof(opts.Value.DoorPathTrap)] = opts.Value.DoorPathTrap,
        })
        {
            Assert.True(File.Exists(pair.Value), userMessage: $"{pair.Key} fnf: {pair.Value}");
        }

        // When
        var g = new DungeonGen(new XunitLogger<DungeonGen>(outputHelper, LogLevel.Information));
        var dungeon = g.Create_dungeon(new Dungeon { seed = 12345 });

        var i = new DungeonImageMapBuilder(new XunitLogger<DungeonImageMapBuilder>(outputHelper, LogLevel.Trace), opts);
        i.CreateMap(dungeon, "test_CreateMap.jpg", showSecrets: false);
        i.CreateMap(dungeon, "test_CreateMap_secret.jpg", showSecrets: true);

        // Then
        Assert.Fail("for logs");
    }
}