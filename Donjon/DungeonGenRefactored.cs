using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Donjon;
#pragma warning disable IDE1006 // Naming Styles

/// <summary>
/// 
/// # Random Dungeon Generator by drow
/// # http://donjon.bin.sh/
/// #
/// # This code is provided under the
/// # Creative Commons Attribution-NonCommercial 3.0 Unported License
/// # http://creativecommons.org/licenses/by-nc/3.0/
/// </summary>
/// <typeparam name="DungeonGenRefactored"></typeparam>
public partial class DungeonGenRefactored(
    IOptions<Settings> settings,
    ILoggerFactory loggerFactory) : IDungeonGenerator
{
    private ILogger<DungeonGenRefactored> logger { get; } = loggerFactory?.CreateLogger<DungeonGenRefactored>()
        ?? NullLogger<DungeonGenRefactored>.Instance;
    public IDungeonRoomIssuer RoomIssuer => MyRoomIssuer;
    // TODO: RoomIssuer should be the point of entry, but IDungeonRoomIssuer is incomplete
    public RoomIdIssuer MyRoomIssuer { get; } = new RoomIdIssuer(
        Options.Create(settings.Value.Rooms),
        Options.Create(settings.Value.Dungeon));

    public IDungeon Create_dungeon()
    {
        var raw = OriginalGeneratorAdapter.CreateLegacy(settings.Value);
        return this.Create_dungeon(raw);
    }
    private static readonly JsonSerializerOptions jsonLoggingOptions = new() { WriteIndented = false };
    protected Random random { get; } = new Random(settings.Value.seed);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dungeon"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public IDungeon Create_dungeon(IDungeon dungeon)
    {
        /*
        //:: sub create_dungeon {
        //::   my ($dungeon) = @_;
        //:: 
        //::   $dungeon->{'n_i'} = int($dungeon->{'n_rows'} / 2);
        //::   $dungeon->{'n_j'} = int($dungeon->{'n_cols'} / 2);
        //::   $dungeon->{'n_rows'} = $dungeon->{'n_i'} * 2;
        //::   $dungeon->{'n_cols'} = $dungeon->{'n_j'} * 2;
        //::   $dungeon->{'max_row'} = $dungeon->{'n_rows'} - 1;
        //::   $dungeon->{'max_col'} = $dungeon->{'n_cols'} - 1;
        //::   $dungeon->{'n_rooms'} = 0;
        //:: 
        //::   my $max = $dungeon->{'room_max'};
        //::   my $min = $dungeon->{'room_min'};
        //::   $dungeon->{'room_base'} = int(($min + 1) / 2);
        //::   $dungeon->{'room_radix'} = int(($max - $min) / 2) + 1;
        //:: 
        //::   $dungeon = &init_cells($dungeon);
        //::   $dungeon = &emplace_rooms($dungeon);
        //::   $dungeon = &open_rooms($dungeon);
        //::   $dungeon = &label_rooms($dungeon);
        //::   $dungeon = &corridors($dungeon);
        //::   $dungeon = &emplace_stairs($dungeon) if ($dungeon->{'add_stairs'});
        //::   $dungeon = &clean_dungeon($dungeon);
        //:: 
        //::   return $dungeon;
        //:: }
        */
        using (logger.BeginScope(nameof(Create_dungeon)))
        {
            var pipe = new Pipeline.Pipeline<IDungeon>(loggerFactory.CreateLogger<Pipeline.Pipeline<IDungeon>>(), nameof(Create_dungeon))
                .RegisterStep(new InitCellsStep(loggerFactory.CreateLogger<InitCellsStep>(), settings))
                .RegisterStep(new EmplaceRoomsStep(loggerFactory.CreateLogger<EmplaceRoomsStep>(), settings: settings, random: random, MyRoomIssuer: MyRoomIssuer))
                .RegisterStep(new EmplaceDoorsStep(loggerFactory.CreateLogger<EmplaceDoorsStep>(), settings, random))
                .RegisterStep(new LabelRoomsStep(loggerFactory.CreateLogger<LabelRoomsStep>()))
                .RegisterStep(new EmplaceCorridorsStep(loggerFactory.CreateLogger<EmplaceCorridorsStep>(), settings, random))
                .RegisterStep(new EmplaceStairsStep(loggerFactory.CreateLogger<EmplaceStairsStep>(), settings, random))
                .RegisterStep(new CleanupStep(loggerFactory.CreateLogger<CleanupStep>(), settings, loggerFactory, random, _w))
                ;
            return pipe.TryInvoke(dungeon, out var after) ? after : throw new Exception();
        }
    }


    private DungeonWriter _w = new();
    public string DescribeDungeonLite(IDungeon dungeon) => _w.DescribeDungeonLite(dungeon);
    public string IndicatePosition(IDungeon d, int i, int j) => _w.IndicatePosition(d, i, j);
}
#pragma warning restore IDE1006 // Naming Styles
