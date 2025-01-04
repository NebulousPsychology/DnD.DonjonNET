using Donjon.Original;

namespace Donjon;
public class DungeonWriter : IDungeonDescriber
{
    public void WriteDungeonGrid(TextWriter writer, IDungeon d, Func<Cellbits, int, int, string>? cellFormatter = null, string separator = " ")
    {
        string Decode(Cellbits cel, int r, int c) => cel switch
        {
            Cellbits cb when cb.HasAnyFlag(Cellbits.DOORSPACE) && d.door.Any(door => door.Coord == (r, c)) => "D",//"D",
            Cellbits cb when cb.HasAnyFlag(Cellbits.DOORSPACE) => "Ð",//"D",
            Cellbits cb when cb.HasAnyFlag(Cellbits.STAIR_UP) => "^",
            Cellbits cb when cb.HasAnyFlag(Cellbits.STAIR_DN) => "v",
            Cellbits cb when cb.HasAnyFlag(Cellbits.ROOM) => "·",
            Cellbits cb when cb.HasAnyFlag(Cellbits.CORRIDOR) => "+",//"◇",
            Cellbits cb when cb.HasAnyFlag(Cellbits.ENTRANCE) => "E",
            Cellbits cb when cb.HasAnyFlag(Cellbits.BLOCKED) => "x",
            Cellbits cb when cb.HasAnyFlag(Cellbits.PERIMETER) => "#",//"⨂",
            Cellbits cb when cb == Cellbits.NOTHING => " ",
            _ => "?"
        };

        cellFormatter ??= Decode;

        var colIndices = Enumerable.Range(0, d.max_col + 1).Select(c => c % 10);
        writer.WriteLine($"        {string.Join(separator, colIndices)}");
        for (int r = 0; r < d.cell.GetLength(0); r++)
        {
            var line = Enumerable.Range(0, d.cell.GetLength(1)).Select(c => cellFormatter(d.cell[r, c], r, c));
            writer.Write("{0,4}:: [", r);
            writer.Write(string.Join(separator, line));
            writer.WriteLine("]");
        }
    }

    private void WritePreamble(TextWriter b, IDungeon dungeon, Settings? s = null)
    {
        if (s is not null)
        {
            b.WriteLine($"seed:{s.seed} {s.Dungeon.n_rows}x{s.Dungeon.n_cols} csz={s.Map.cell_size} dun{s.Dungeon.dungeon_layout} cor{s.Corridors.corridor_layout}");
            b.WriteLine($"nrooms:{dungeon.n_rooms} actual:{dungeon.room.Count} last='{dungeon.last_room_id?.ToString() ?? "nul"}' sz({s.Rooms.room_min}..{s.Rooms.room_max})");
        }
        else if (dungeon is Dungeon d)
        {
            b.WriteLine($"seed:{d.seed} {d.n_rows}x{d.n_cols} csz={d.cell_size} dun{d.dungeon_layout} cor{d.corridor_layout}");
            b.WriteLine($"nrooms:{d.n_rooms} actual:{d.room.Count} last='{d.last_room_id?.ToString() ?? "nul"}' sz({d.room_min}..{d.room_max})");
        }
        foreach (var item in dungeon.room)
        {
            b.Write($"    key'{item.Key}' [id{item.Value.id}] | ({item.Value.north},{item.Value.west})..({item.Value.south},{item.Value.east})");
            b.WriteLine($" | {item.Value.height}v x {item.Value.width}h = {item.Value.area}");
        }


        b.WriteLine($"ndoor:{dungeon.door.Count}");
        foreach (var item in dungeon.door.OrderBy(dr => dr.col).OrderBy(dr => dr.row))
        {
            b.WriteLine($"    ({item.row,3},{item.col,3}) {item.key} oid{item.out_id?.ToString() ?? "_"} {item.type} :'{item.desc}' :: {dungeon.cell[item.row, item.col] & ~Cellbits.LABELSPACE}");
        }

        b.WriteLine($"nStair:{dungeon.stair.Count}");
        foreach (var item in dungeon.stair)
        {
            b.WriteLine(item is null ? "    null" : $"    ({item.row},{item.col}) {item.key} next:({item.next_row},{item.next_col})");
        }
    }

    public string DescribeDungeon(Dungeon d, int size = 3)
    {
        using var b = new StringWriter();
        WritePreamble(b, d);
        WriteDungeonGrid(b, d, (cel, r, c) => string.Format($"[{{0,{size}}}]", cel.Summarize()));
        return b.ToString();
    }

    public string DescribeDungeonLite(IDungeon dungeon)
    {
        using var b = new StringWriter();
        WritePreamble(b, dungeon);
        WriteDungeonGrid(b, dungeon);
        return b.ToString();
    }
    public string IndicatePosition(Dungeon d, int i, int j)
    {
        using var b = new StringWriter();
        WriteDungeonGrid(b, d, (cel, r, c) => r == i && c == j ? "X" : "•");
        return b.ToString();
    }
}