using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Xunit.Abstractions;

namespace Donjon.Test;

public class DungeonGeneratorParityTest(ITestOutputHelper output)
    : Utilities.HostedTestBase<DungeonGeneratorParityTest>(output, LogLevel.Information),
     IDungeonDescriber<string>
{
    IOptions<Settings> settings = Substitute.For<IOptions<Settings>>();

    Settings CreateTestSettings(int sd) => new Settings()
    {
        Dungeon = new() { dungeon_layout = "None", n_rows = 11, n_cols = 11, },
        Map = new() { map_style = "Standard", cell_size = 18 },
        Rooms = new() { room_layout = Original.RoomLayout.Scattered, room_min = 2, room_max = 5, },
        Corridors = new() { corridor_layout = "Bent", remove_deadends = 50, add_stairs = 2, },
        seed = sd,
    };
    protected override Dictionary<LogLevel, IEnumerable<string>> Filters => new()
    {
        [LogLevel.Trace] = ["Donjon.Original.DungeonGen"],
    };
    protected override void PerformServiceConfig(HostBuilderContext context, IServiceCollection services)
    {
        base.PerformServiceConfig(context, services);
        services.AddSingleton<IOptions<Settings>>(settings);
        // services.AddScoped<DungeonGenRefactored>();
        services.AddScoped<OriginalGeneratorAdapter>();
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
            var s = TestHost.Services.GetRequiredService<IOptions<Settings>>();
            Logger.LogInformation("{name} :: got settings: {s}", name, s.Value.ToJson(true));

            Original.DungeonGen rawOriginalGen = new(TestHost.Services.GetRequiredService<ILogger<Original.DungeonGen>>());
            OriginalGeneratorAdapter adaptOriginalGen = TestHost.Services.GetRequiredService<OriginalGeneratorAdapter>();
            // var refactorGen = TestHost.Services.GetRequiredService<DungeonGenRefactored>();

            // When
            Original.Dungeon raw_dungeon0 = rawOriginalGen.Create_dungeon(OriginalGeneratorAdapter.CreateLegacy(s.Value));
            Original.Dungeon raw_dungeon1 = rawOriginalGen.Create_dungeon(OriginalGeneratorAdapter.CreateLegacy(s.Value));
            Logger.LogInformation("json:\n{j}", raw_dungeon0.ToJson(true));
            IDungeon dungeon0 = new OriginalGeneratorAdapter.OriginalDungeonAdapter(raw_dungeon0);
            IDungeon dungeon1 = new OriginalGeneratorAdapter.OriginalDungeonAdapter(raw_dungeon1);

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

            var imglog = TestHost.Services.GetRequiredService<ILogger<ImageTools.DungeonImageMapBuilder>>();
            var mb = new Donjon.ImageTools.DungeonImageMapBuilder(imglog, opts);
            mb.CreateMap(raw_dungeon0, $"test_CreateMap_{nameof(SelfSimilarParity)}_secret.jpg", showSecrets: true);

            // Then 
            Assert.Equal(dungeon0, dungeon1, DungeonEqualityComparer.Instance); 
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
    public void RefactoredGenerationParity(string name, int seed)
    {
        using (Logger.BeginScope(nameof(RefactoredGenerationParity)))
        {
            // Given
            settings.Value.Returns(CreateTestSettings(seed));
            var s = TestHost.Services.GetRequiredService<IOptions<Settings>>();
            Logger.LogInformation("{name} :: got settings: {s}", name, s.Value.ToJson(true));

            Original.DungeonGen rawOriginalGen = new(TestHost.Services.GetRequiredService<ILogger<Original.DungeonGen>>());
            OriginalGeneratorAdapter adaptOriginalGen = TestHost.Services.GetRequiredService<OriginalGeneratorAdapter>();
            // var refactorGen = TestHost.Services.GetRequiredService<DungeonGenRefactored>();

            // When
            Original.Dungeon raw_dungeon0 = rawOriginalGen.Create_dungeon(OriginalGeneratorAdapter.CreateLegacy(s.Value));
            IDungeon dungeon0 = new OriginalGeneratorAdapter.OriginalDungeonAdapter(raw_dungeon0);
            IDungeon dungeon1 = adaptOriginalGen.Create_dungeon();
            // IDungeon dungeon2 = refactorGen.Create_dungeon();
            Logger.LogInformation("COMPARISON");
            using (Logger.BeginScope("Conclusion"))
            {
                Logger.LogInformation("before:\n{d0}", DescribeDungeonLite(dungeon0));
                Logger.LogInformation("_adapt:\n{d0}", DescribeDungeonLite(dungeon1));
                // Logger.LogInformation("_after:\n{d0}", dw.DescribeDungeonLite(dungeon2));
            }

            // Then
            Assert.Equal(dungeon0, dungeon1, DungeonEqualityComparer.Instance);
            // Assert.Equal(dungeon1, dungeon2, DungeonEqualityComparer.Instance);
        }
    }

    public string DescribeDungeonLite(IDungeon dungeon)
    {
        DungeonWriter dw = new();
        return $"{dw.DescribeDungeonLite(dungeon)}";
    }
}