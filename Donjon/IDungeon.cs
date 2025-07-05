namespace Donjon;

using Donjon.Original;

using TRoomId = int;

#pragma warning disable IDE1006 // Naming Styles
public interface IDungeon : IDungeonDimensional, IDungeonRoomIssuer
{
    public Cellbits[,] cell { get; }
    public Dictionary<string, int>? connect { get; }
    public Dictionary<TRoomId, IDungeonRoom> room { get; }
    public List<DoorData> door { get; }
    public List<StairEnd?> stair { get; }
}

public class DungeonEqualityComparer : IEqualityComparer<IDungeon>
{
    public bool Equals(IDungeon? x, IDungeon? y)
    {
        if (x is null || y is null) return false;
        if (false == (x.connect ?? []).SequenceEqual(y.connect ?? [])) return false;
        if (false == (x.room ?? []).SequenceEqual(y.room ?? [])) return false;
        if (false == (x.door ?? []).SequenceEqual(y.door ?? [])) return false;
        if (false == (x.stair ?? []).SequenceEqual(y.stair ?? [])) return false;
        if (x.cell is null || y.cell is null) return false;
        if (x.cell.GetLength(0) != y.cell.GetLength(0)) return false;
        if (x.cell.GetLength(1) != y.cell.GetLength(1)) return false;
        if (x.cell.Length != y.cell.Length) return false;
        var raster = Enumerable.Range(0, x.cell.GetLength(0))
            .SelectMany(r => Enumerable.Range(0, x.cell.GetLength(1)).Select(c => (r, c)));
        foreach (var (i, j) in raster)// Dim2d.RangeInclusive(0, x.cell.GetLength(0) - 1, 0, x.cell.GetLength(1) - 1)
        {
            if (x.cell[i, j] != y.cell[i, j]) return false;
        }
        return true;
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
