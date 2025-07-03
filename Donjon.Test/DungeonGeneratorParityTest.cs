using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Xunit.Abstractions;

namespace Donjon.Test;

public class DungeonGeneratorParityTest(ITestOutputHelper output) : Utilities.HostedTestBase<DungeonGeneratorParityTest>(output)
{
    IOptions<Settings> settings = Substitute.For<IOptions<Settings>>();

    Settings S(int sd) => new Settings()
    {
        Dungeon = new() { dungeon_layout = "None", n_rows = 11, n_cols = 11, },
        Map = new() { map_style = "Standard", cell_size = 18 },
        Rooms = new() { room_layout = Original.RoomLayout.Scattered, room_min = 3, room_max = 9, },
        Corridors = new() { corridor_layout = "Bent", remove_deadends = 50, add_stairs = 2, },
        seed = sd,
    };

    protected override void PerformServiceConfig(HostBuilderContext context, IServiceCollection services)
    {
        base.PerformServiceConfig(context, services);
        services.AddSingleton<IOptions<Settings>>(settings);
        services.AddScoped<DungeonGenRefactored>();
        services.AddScoped<Original.DungeonGen>();
    }

    [Theory]
    [InlineData("a", 3)]
    [InlineData("b", 4)]
    public void RefactoredGenerationParity(string name, int seed)
    {
        using (Logger.BeginScope(nameof(RefactoredGenerationParity)))
        {
            settings.Value.Returns(S(seed));

            var s = TestHost.Services.GetRequiredService<IOptions<Settings>>();
            Logger.LogInformation("{name} :: got settings: {s}", name, s.Value.ToJson(true));
            var d0 = Settings.CreateLegacy(s.Value);
            Logger.LogInformation("{name} :: got dungeon: {s}", name, d0.ToJson(true));
            // Given
            var originalGen = TestHost.Services.GetRequiredService<Original.DungeonGen>();
            var refactorGen = TestHost.Services.GetRequiredService<DungeonGenRefactored>();

            // When
            Original.Dungeon dungeon0 = originalGen.Create_dungeon(Settings.CreateLegacy(s.Value));
            IDungeon dungeon1 = refactorGen.Create_dungeon(Settings.CreateLegacy(s.Value));
            Logger.LogInformation("COMPARISON");
            using (Logger.BeginScope("Conclusion"))
            {
                Logger.LogInformation("before: {d0}", originalGen.DescribeDungeonLite(dungeon0));
                Logger.LogInformation(" after: {d0}", refactorGen.DescribeDungeonLite(dungeon1));
            }

            // Then
            Assert.Equal(dungeon0, dungeon1, new DungeonEqualityComparer());
        }
    }
}