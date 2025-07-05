using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Xunit.Abstractions;

namespace Donjon.Test;

public class DungeonGeneratorParityTest(ITestOutputHelper output)
    : Utilities.HostedTestBase<DungeonGeneratorParityTest>(output),
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

    protected override void PerformServiceConfig(HostBuilderContext context, IServiceCollection services)
    {
        base.PerformServiceConfig(context, services);
        services.AddSingleton<IOptions<Settings>>(settings);
        // services.AddScoped<DungeonGenRefactored>();
        services.AddScoped<OriginalGeneratorAdapter>();
    }

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
            var dec = new DungeonEqualityComparer(TestHost.Services.GetRequiredService<ILogger<DungeonEqualityComparer>>());
            Assert.Equal(dungeon0, dungeon1, dec);
            // Assert.Equal(dungeon1, dungeon2, new DungeonEqualityComparer());
        }
    }

    public string DescribeDungeonLite(IDungeon dungeon)
    {
        DungeonWriter dw = new();
        return $"{dw.DescribeDungeonLite(dungeon)}";
    }
}