

using Donjon.Original;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using SixLabors.ImageSharp;

using Xunit.Abstractions;

namespace Donjon.Test;

public class DungeonGeneratorParityTest(ITestOutputHelper output)
    : Utilities.HostedTestBase<DungeonGeneratorParityTest>(output, LogLevel.Debug),
     IDungeonDescriber<string>
{
    IOptions<Settings> settings = Substitute.For<IOptions<Settings>>();

    Settings CreateTestSettings(int seed) => new Settings()
    {
        Dungeon = new() { dungeon_layout = "None", n_rows = 11, n_cols = 11, },
        Map = new() { map_style = "Standard", cell_size = 18 },
        Rooms = new() { room_layout = Original.RoomLayout.Scattered, room_min = 2, room_max = 5, },
        Corridors = new() { corridor_layout = Original.CorridorLayout.Bent, remove_deadends = 50, add_stairs = 2, },
        seed = seed,
    };
    protected override Dictionary<LogLevel, IEnumerable<string>> Filters => new()
    {
        [LogLevel.Trace] = [
            "Donjon.DungeonGenRefactored.EmplaceRoomsStep",
            ],
        [LogLevel.Debug] = [
            "Donjon.DungeonGenRefactored",
        ],
        [LogLevel.Information] = [
            "Donjon.Test.DungeonGeneratorParityTest",
            "Donjon.Original.DungeonGen",
            // "Donjon.Test.*",
        ],
    };
    protected override void PerformServiceConfig(HostBuilderContext context, IServiceCollection services)
    {
        base.PerformServiceConfig(context, services);
        services.AddSingleton<IOptions<Settings>>(settings);
        services.AddScoped<DungeonWriter>();
        services.AddTransient<DungeonEqualityComparer>();
        services.AddScoped<DungeonGenRefactored>();
        services.AddScoped<OriginalGeneratorAdapter>();
        services.AddScoped<Original.DungeonGen>();
        services.AddScoped<OriGeneratorPipelineAdapter>();
    }

    /// <summary>
    /// ensure a raw OriginalDungeon is the same as another, when using the same settings.
    /// (this validates the DungeonEqualityComparer)
    /// </summary>
    /// <param name="name"></param>
    /// <param name="seed"></param>
    [Theory]
    [InlineData("a", 3)]
    [InlineData("b", 4)]
    public void SelfSimilarParity(string name, int seed)
    {
        using (Logger.BeginScope(nameof(RefactoredGenerationParity)))
        { // Given
            settings.Value.Returns(CreateTestSettings(seed));
            var s = Services.GetRequiredService<IOptions<Settings>>();
            Logger.LogInformation("{name} :: got settings: {s}", name, s.Value.ToJson(true));

            Original.DungeonGen rawOriginalGen = Services.GetRequiredService<Original.DungeonGen>();
            var adaptOriginalGen = Services.GetRequiredService<OriginalGeneratorAdapter>();
            // var refactorGen = Services.GetRequiredService<DungeonGenRefactored>();

            // When
            Original.Dungeon raw_dungeon0 = rawOriginalGen.Create_dungeon(OriginalDungeonAdapter.CreateLegacy(s.Value));
            Original.Dungeon raw_dungeon1 = rawOriginalGen.Create_dungeon(OriginalDungeonAdapter.CreateLegacy(s.Value));
            Logger.LogInformation("json:\n{j}", raw_dungeon0.ToJson(true));
            IDungeon dungeon0 = new OriginalDungeonAdapter(raw_dungeon0);
            IDungeon dungeon1 = new OriginalDungeonAdapter(raw_dungeon1);

            // with image
            var opts = Substitute.For<IOptions<ImageTools.ImageMapOptions>>();
            opts.Value.Returns(new ImageTools.ImageMapOptions
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

            var imglog = Services.GetRequiredService<ILogger<ImageTools.DungeonImageMapBuilder>>();
            var mb = new Donjon.ImageTools.DungeonImageMapBuilder(imglog, opts);
            mb.CreateMap(raw_dungeon0, $"test_CreateMap_{nameof(SelfSimilarParity)}_secret.jpg", showSecrets: true);

            // Then 
            var loggingCompare = new DungeonEqualityComparer(LoggerFactory);
            Assert.Equal(dungeon0, dungeon1, loggingCompare);
        }
    }

    /// <summary>
    /// Ensure that Original.DungeonGen and  OriginalGeneratorAdapter (TODO: and DungeonGenRefactored)
    /// </summary>
    /// <param name="name"></param>
    /// <param name="seed"></param>
    [Theory]
    [InlineData("a", 3)]
    [InlineData("b", 4)]
    public void RefactoredGenerationEntry(string name, int seed)
    {
        using (Logger.BeginScope(nameof(RefactoredGenerationEntry)))
        {
            // Given
            settings.Value.Returns(CreateTestSettings(seed));
            var s = Services.GetRequiredService<IOptions<Settings>>();
            Logger.LogInformation("{name} :: got settings: {s}", name, s.Value.ToJson(true));
            var refactorGen = Services.GetRequiredService<DungeonGenRefactored>();

            // When
            try
            {
                IDungeon dungeon2 = refactorGen.Create_dungeon();
                Logger.LogInformation("COMPARISON");
                using (Logger.BeginScope("Conclusion"))
                {
                    Logger.LogInformation("_after:\n{d0}", DescribeDungeonLite(dungeon2));
                }
            }
            catch (Exception any)
            {
                Logger.LogWarning(exception: any, "Finish {e}", any);
                throw;
            }

            // Then
            // ...
        }
    }

    /// <summary>
    /// Skips emplace_room_collisiontest
    /// </summary>
    /// <param name="r"></param>
    /// <param name="c"></param>
    /// <param name="h"></param>
    /// <param name="w"></param>
    /// <param name="x"></param>
    /// <param name="sampleRow"></param>
    /// <param name="sampleRowCells"></param>
    /// <param name="sampleCol"></param>
    /// <param name="sampleColCells"></param>
    [Theory]
    // [InlineData(new object[]{new SixLabors.ImageSharp.Rectangle(y: 2, x: 3, height: 4, width: 5)})]
    [InlineData([3, 4, 5, 6, 'a', 3, 6, 9, 5])]
    [InlineData([3, 4, 1, 1, 'a', 3, 1, 4, 1])]
    [InlineData([3, 4, 0, 0, 'a', 0, 0, 0, 0], Skip = "throws instead")]
    // TODO: cases involving rectangles exceeding dungeon bounds
    public void ManualRoomPlacement(int r, int c, int h, int w, char x, int sampleRow, int sampleRowCells, int sampleCol, int sampleColCells)
    {
        settings.Value.Returns(CreateTestSettings(123));
        var s = Services.GetRequiredService<IOptions<Settings>>();
        // Given
        OriGeneratorPipelineAdapter adaptOriginalGen = Services.GetRequiredService<OriGeneratorPipelineAdapter>();
        DungeonGenRefactored refactorGen = Services.GetRequiredService<DungeonGenRefactored>();
        var d = OriginalGeneratorAdapter.CreateLegacy(settings.Value);
        Logger.LogInformation("before:\n{d0}", DescribeDungeonLite(d));

        var rand = new Random(s.Value.seed);
        DungeonGenRefactored.EmplaceRoomsStep ers = new(LoggerFactory.CreateLogger<DungeonGenRefactored.EmplaceRoomsStep>(), s, rand, refactorGen.MyRoomIssuer);
        // When
        // var d2 = ers.emplace_room(d, new((1, 1)));
        var rect = new Rectangle(x: c, y: r, width: w, height: h); // 3,2  to  6,8 inclusive =(3,2)+(3,6)=(3,2)+(h-1,w+1?)
        ers.emplace_room_carve(d, 0, new Realspace<Rectangle>(rect));
        Logger.LogInformation("carve:\n{d0}", DescribeDungeonLite(d));

        ers.emplace_room_BlockPerimeter(d, rect);
        Logger.LogInformation("perim:\n{d0}", DescribeDungeonLite(d));
        Logger.LogInformation("perim:\n{d0}", DescribeDungeon(d));

        // Then
        var rcount = Enumerable
                    .Range(0, d.cell.GetLength(0))
                    .Select(c => d.cell[sampleRow, c])
                    .Count(c => c.HasAnyFlag(Original.Cellbits.ROOM));
        Assert.Equal(sampleRowCells, rcount);
        var ccount = Enumerable
                    .Range(0, d.cell.GetLength(1))
                    .Select(r => d.cell[r, sampleCol])
                    .Count(c => c is Original.Cellbits.ROOM);
        Assert.Equal(sampleColCells, ccount);
    }

    /// <summary>
    /// Ensure that Original.DungeonGen and  OriginalGeneratorAdapter (TODO: and DungeonGenRefactored)
    /// </summary>
    /// <param name="name"></param>
    /// <param name="seed"></param>
    [Theory]
    [InlineData("a", 3)]
    [InlineData("b", 4)]
    public void RefactoredGenerationParity(string name, int seed)
    {
        using (Logger.BeginScope(nameof(RefactoredGenerationParity)))
        {
            // Given
            settings.Value.Returns(CreateTestSettings(seed));
            var s = Services.GetRequiredService<IOptions<Settings>>();
            Logger.LogInformation("{name} :: got settings: {s}", name, s.Value.ToJson(true));

            // Original.DungeonGen rawOriginalGen = Services.GetRequiredService<Original.DungeonGen>();
            OriGeneratorPipelineAdapter adaptOriginalGen = Services.GetRequiredService<OriGeneratorPipelineAdapter>();
            DungeonGenRefactored refactorGen = Services.GetRequiredService<DungeonGenRefactored>();

            // When
            // Original.Dungeon raw_dungeon0 = rawOriginalGen.Create_dungeon(OriginalDungeonAdapter.CreateLegacy(s.Value));
            // IDungeon dungeon0 = new OriginalDungeonAdapter(raw_dungeon0);
            IDungeon dungeon1 = adaptOriginalGen.Create_dungeon();
            IDungeon dungeon2 = refactorGen.Create_dungeon();
            Logger.LogInformation("COMPARISON");
            using (Logger.BeginScope("Conclusion"))
            {
                // Logger.LogInformation("before:\n{d0}", DescribeDungeonLite(dungeon0));
                Logger.LogInformation("_adapt:\n{d0}", DescribeDungeon(dungeon1));
                Logger.LogInformation("_after:\n{d0}", DescribeDungeon(dungeon2));
                Logger.LogInformation("_adapt:\n{d0}", DescribeDungeonLite(dungeon1));
                Logger.LogInformation("_after:\n{d0}", DescribeDungeonLite(dungeon2));
            }

            // Then 
            // Assert.Equal(dungeon0, dungeon1, loggingCompare);
            Assert.Equal(dungeon1, dungeon2, Services.GetRequiredService<DungeonEqualityComparer>());
        }
    }

    /// <summary>
    /// demonstrate that the <see cref="OriGeneratorPipelineAdapter"/> correctly reproduces Original.DungeonGen, and can be used for test
    /// </summary>
    [Fact]
    public void DParity()
    {
        // Given
        settings.Value.Returns(CreateTestSettings(4));
        Original.DungeonGen g = this.Services.GetRequiredService<Original.DungeonGen>();
        OriGeneratorPipelineAdapter g2 = Services.GetRequiredService<OriGeneratorPipelineAdapter>();

        // When
        var d1_ = OriginalDungeonAdapter.CreateLegacy(settings.Value);
        var d1 = g.Create_dungeon(d1_);
        var d1a = new OriginalDungeonAdapter(d1);
        var d2 = g2.Create_dungeon();

        // Then
        using (Logger.BeginScope("Conclusion"))
        {
            Logger.LogInformation("before:\n{d0}", DescribeDungeonLite(d1a));
            Logger.LogInformation("_adapt:\n{d0}", DescribeDungeonLite(d2));
        }

        Assert.Equal(d1a, d2, Services.GetRequiredService<DungeonEqualityComparer>());
    }
    public string DescribeDungeon(IDungeon dungeon)
    {
        var dw = Services.GetRequiredService<DungeonWriter>();
        return $"{dw.DescribeDungeon(dungeon)}";
    }
    public string DescribeDungeonLite(IDungeon dungeon)
    {
        DungeonWriter dw = new();
        return $"{dw.DescribeDungeonLite(dungeon)}";
    }
}
