namespace Donjon;

using Donjon.Original;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using TRoomId = int;

#pragma warning disable IDE1006 // Naming Styles
public interface IDungeon : IDungeonDimensional, IDungeonRoomIssuer
{
    public Cellbits[,] cell { get; }
    public IDictionary<string, int>? connect { get; }
    public IDictionary<TRoomId, IDungeonRoom> room { get; }
    public IList<DoorData> door { get; }
    public IList<StairEnd?> stair { get; }
}

public class DungeonEqualityComparer(ILoggerFactory loggerFactory) : IEqualityComparer<IDungeon>
{
    public static DungeonEqualityComparer Instance { get; } = new(NullLoggerFactory.Instance);
    private ILogger<DungeonEqualityComparer> logger { get; } = loggerFactory.CreateLogger<DungeonEqualityComparer>();
    bool EqualRooms(IDictionary<TRoomId, IDungeonRoom> x, IDictionary<TRoomId, IDungeonRoom> y)
    {
        if (x is null || y is null) { logger.LogDebug("null roomdict"); return false; }
        if (x.Count != y.Count)
        {
            logger.LogDebug("count differs ({x},{y})", x.Count, y.Count); return false;
        }
        return x.Keys.Union(y.Keys)
            .All(key => x.TryGetValue(key, out var value)
                && y.TryGetValue(key, out var value2)
                && value.Equals(value2));
    }

    public bool Equals(IDungeon? x, IDungeon? y)
    {
        var equalityCheck = ((IDungeon? a, IDungeon? b) xy, Func<IDungeon?, IDungeon?, bool> valid, string failureMsg) =>
        {
            if (valid(xy.a, xy.b)) return xy;
            logger.LogWarning("rejected: {msg}", failureMsg);
            throw new InvalidDataException(failureMsg);
        };
        var emptyconnect = Enumerable.Empty<KeyValuePair<string, int>>();
        var p = new Pipeline.Pipeline<(IDungeon?, IDungeon?)>(loggerFactory.CreateLogger<Pipeline.Pipeline<(
            IDungeon?, IDungeon?)>>(), nameof(DungeonEqualityComparer))
            .RegisterStep(xy => equalityCheck(xy, (x, y) => x is not null && y is not null, "null rejected"))
            .RegisterStep(xy => equalityCheck(xy, (x, y) => x?.cell is not null && y?.cell is not null, "null cell rejected"))
            .RegisterStep(xy => equalityCheck(xy, (x, y) => (x?.connect ?? emptyconnect).SequenceEqual(y?.connect ?? emptyconnect), "connect"))
            .RegisterStep(xy => equalityCheck(xy, (x, y) => EqualRooms(x?.room!, y?.room!), "rooms"))
            .RegisterStep(xy => equalityCheck(xy, (x, y) => (x?.door ?? []).SequenceEqual(y?.door ?? []), "door"))
            .RegisterStep(xy => equalityCheck(xy, (x, y) => (x?.stair ?? []).SequenceEqual(y?.stair ?? []), "stair"))
            .RegisterStep(xy => equalityCheck(xy, (x, y) => x?.cell.GetLength(0) == y?.cell.GetLength(0), "dim0"))
            .RegisterStep(xy => equalityCheck(xy, (x, y) => x?.cell.GetLength(1) == y?.cell.GetLength(1), "dim1"))
            .RegisterStep(xy => equalityCheck(xy, (x, y) => x?.cell.Length == y?.cell.Length, "length"))
            .RegisterStep(xy => equalityCheck(xy, (x, y) =>
            {
                try
                {
                    var raster = Enumerable.Range(0, x!.cell.GetLength(0))
                        .SelectMany(r => Enumerable.Range(0, x.cell.GetLength(1)).Select(c => (r, c)));
                    foreach (var (i, j) in raster)// Dim2d.RangeInclusive(0, x.cell.GetLength(0) - 1, 0, x.cell.GetLength(1) - 1)
                    {
                        if (x.cell[i, j] != y!.cell[i, j])
                        {
                            logger.LogWarning("cell content differs, starting at {ij}", (i, j));
                            return false;
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.LogWarning("cell content differs, exceptionally: {e}", e);
                    return false;
                }
                return true;

            }, "contents differ"));
        return p.TryInvoke((x, y), out _);
    }

    public int GetHashCode(IDungeon obj)
    {
        return obj.GetHashCode();
    }
}

public interface IDungeonDimensional
{
    /// <summary>half rows, will be even by intcast</summary>
    public int n_i { get; }

    /// <summary>half cols, will be even by intcast</summary>
    public int n_j { get; }

    /// <summary>inclusive-max index of rows (will be even by -1)</summary>
    public int max_row { get; }

    /// <summary>inclusive-max index of cols (will be even by -1)</summary>
    public int max_col { get; }
}
