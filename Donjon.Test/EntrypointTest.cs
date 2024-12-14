
using System.Text.Json;

using Donjon.Test.Utilities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit.Abstractions;

namespace Donjon.Test;

public class EntrypointTest(ITestOutputHelper outputHelper)
: HostedTestBase<EntrypointTest>(outputHelper)
{
    readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
        MaxDepth = 20
    };

    public static IEnumerable<object[]> SplitCountData() { yield break; }

    [Theory, MemberData(nameof(SplitCountData))]
    [InlineData("none", 12345, 9)]//  
    [InlineData("doorway2nothing", 34392, 15)]//  seed:34392 39x39 csz=18  has a doorway to nothing!?
    [InlineData("doorway2nothing", 36054, 13)]//  seed:36054 39x39 csz=18 dunNone corBent nrooms:8 actual:8 last='8' sz(3..9)
    [InlineData("detachedroom", 55585, 8)]//  seed:55585 39x39 csz=18 dunNone corBent nrooms:8 actual:8 last='8' sz(3..9) detached room
    [InlineData("surviving entry/doorway2nothing", 35, 15)]//   seed:35 39x39 csz=18 dunNone corBent nrooms:13 actual:13 last='13' sz(3..9) (surviving Entry)
    public void SpecificSeeds(string concern, int seed, int expectedDoorCount = -1)
    {
        var g = new DungeonGen(LoggerFactory.CreateLogger<DungeonGen>());
        Dungeon d = new() { seed = seed, };
        Assert.NotNull(d.cell);
        Assert.NotNull(d.random);
        Assert.Equal(d.n_rows, d.cell.GetLength(0));
        Assert.Equal(d.n_cols, d.cell.GetLength(1));
        // Logger.LogInformation("{description}", g.DescribeDungeon(d));

        d = g.Create_dungeon(d);
        Logger.LogInformation("{description}", g.DescribeDungeonLite(d));
        Logger.LogInformation("{description}", concern);

        // Then
        var indices = Enumerable.Range(0, d.cell.GetLength(0))
            .SelectMany(r => Enumerable.Range(0, d.cell.GetLength(1)).Select(c => (r, c)));
        Assert.NotEmpty(indices);
        Assert.Equal(d.cell.Length, indices.Count());
        // door counting 
        var doorIndices = indices.Where(idx => d.cell[idx.r, idx.c].HasAnyFlag(Cellbits.DOORSPACE));
        Assert.NotEmpty(doorIndices);
        int countDoorsByCell = doorIndices.Count();
        int countDoorsByObject = d.door.Count;
        var indicesNotInObjs = doorIndices.Except(d.door.Select(door => door.Coord)).ToArray();
        var objsNotInIndices = d.door.Select(door => door.Coord).Except(doorIndices).ToArray();
        Logger.LogInformation("{c} cells Notin= {a}", indicesNotInObjs.Count(), string.Join(",", indicesNotInObjs));
        Logger.LogInformation("{c} objs Notin cells = {a}", objsNotInIndices.Count(), string.Join(",", objsNotInIndices));

        Assert.True(countDoorsByCell == countDoorsByObject, $"doors: {countDoorsByCell} cel != {countDoorsByObject} obj");
        Assert.Equal(expectedDoorCount, countDoorsByObject);
        Assert.Equal(expectedDoorCount, countDoorsByCell);
        Assert.True(expectedDoorCount >= d.n_rooms, "there should be at least as many doors as rooms");
        // and doors should be GOOD: one neighboring Room, one neighboring corridor, 
        foreach (var doorcell in doorIndices)
        {
            DoorData data = Assert.Single(d.door, c => doorcell.r == c.row && doorcell.c == c.col);
            AssertGoodDoorCell(d, (doorcell.r, doorcell.c));
        }

        // Assert.Fail($"{concern} : Exited without Failure");
    }

    void AssertGoodDoorCell(Dungeon d, (int r, int c) coord)
    {
        // if (r<0||r>=d.cell.GetLength(0)) 
        static bool OutOfGrid((int r, int c) coords, Cellbits[,] grid)
            => coords.r < 0 || coords.r >= grid.GetLength(0)
            || coords.c < 0 || coords.c >= grid.GetLength(1);
        var neighboring = DungeonGen.directions_allinone.Select(o => new
        {
            dir = o.Key,
            o.Value.opposite,
            ij = (i: o.Value.i, j: o.Value.j),
            rc = (r: o.Value.i + coord.r, c: o.Value.j + coord.c),
            r = o.Value.i + coord.r,
            c = o.Value.j + coord.c,
        }).Where(n => !OutOfGrid(n.rc, d.cell));
        if (neighboring.Any(n => d.cell[n.r, n.c].HasAnyFlag(Cellbits.ROOM)))
        {
            //expect room-corridor
            //? need to enforce opposing-side?
            Assert.Contains(neighboring, filter: n => d.cell[n.r, n.c].HasAnyFlag(Cellbits.ROOM));
            Assert.Contains(neighboring, n => d.cell[n.r, n.c].HasAnyFlag(Cellbits.CORRIDOR));
        }
        else
        {
            // expect corridor-corridor or room-room
            var cors = neighboring.Count(n => d.cell[n.r, n.c].HasAnyFlag(Cellbits.CORRIDOR));
            var rooms = neighboring.Count(n => d.cell[n.r, n.c].HasAnyFlag(Cellbits.ROOM));
            Assert.Equal(2, cors + rooms);
        }
    }

    [Fact]
    public void RandomlyProbeDungeonSeeds()
    {
        var g = new DungeonGen(NullLogger<DungeonGen>.Instance);
        Random seedgen = new();
        List<dynamic> interestingOnes = [];
        var seeds = Enumerable.Concat([12345], Enumerable.Range(0, 200).Select(s => seedgen.Next(99999)));
        foreach (var seed in seeds)
        {
            Dungeon d = new() { seed = seed, };
            Assert.NotNull(d.cell);
            Assert.NotNull(d.random);
            Assert.Equal(d.n_rows, d.cell.GetLength(0));
            Assert.Equal(d.n_cols, d.cell.GetLength(1));
            Logger.LogInformation("{description}", g.DescribeDungeon(d));
            try
            {
                var d2 = g.Create_dungeon(d);
                Logger.LogInformation("{description}", g.DescribeDungeonLite(d2));
            }
            catch (Exception exception)
            {
                interestingOnes.Add(new { exception, seed, });
            }
        }
        Logger.LogInformation("seeds tested: [{s}]", string.Join(",", seeds));
        Logger.LogInformation(JsonSerializer.Serialize(interestingOnes, options: jsonOptions));
        Assert.Empty(interestingOnes);
    }


    [Fact]
    public void RandomlyProbeDungeonSeeds_AlwaysLog()
    {
        RandomlyProbeDungeonSeeds();
    }

    [Fact(Skip = "manual confirmation")]
    public void LogsToDebugAndResults()
    {
        // Information[0]<HelloStringScope> info
        // Information[0]<{ hello = object, scope = 2 }>HelloStringScope> info2
        using (Logger.BeginScope("HelloStringScope"))
        {
            Logger.LogInformation("info");
            using (Logger.BeginScope(new { hello = "object", scope = 2 }))
            {
                Logger.LogInformation("info2");
            }
        }
    }
}