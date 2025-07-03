// Adapted from https://donjon.bin.sh/code/dungeon/dungeon.pl
// https://creativecommons.org/licenses/by-nc/3.0/
namespace Donjon.Original;

public static class CellbitsExtensions
{
    /// <summary> if ROOM flag is set, unshift and return the bits of ROOM_ID filter </summary>
    public static bool TryGetRoomId(this Cellbits cell, out int id)
    {
        if (cell.HasFlag(Cellbits.ROOM))
        {
            id = (int)(cell & Cellbits.ROOM_ID) >> 6;
            return true;
        }
        id = 0;
        return false;
    }

    [Obsolete("Untested")]
    public static void TrySetRoomId(this ref Cellbits cell, int id)
    {
        cell |= Cellbits.ROOM | (Cellbits)((id << 6) & (int)Cellbits.ROOM_ID);
    }

    /// <summary>   </summary>
    public static bool TryGetLabel(this Cellbits cell, out byte label)
    {
        if (cell.HasFlag(Cellbits.LABEL))
        {
            label = (byte)(((uint)cell & (uint)Cellbits.LABEL) >> 24);
            return true;
        }
        label = 0;
        return false;
    }
    /// <summary>
    /// Bitwise-Or's a char into the top 8 bits of the UInt32
    /// FIXME: `char` is UTF16; a 2-byte size
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="c"></param>
    /// <returns></returns>
    public static Cellbits SetLabel(this Cellbits cell, char c) => cell | (Cellbits)(c << 24);

    /// <summary>determine if any of the bits set in mask are set</summary>
    /// <returns>true if any of the bits set in mask are set in the current instance; otherwise false</returns>
    public static bool HasAnyFlag(this Cellbits cell, Cellbits mask) => (cell & mask) != 0;
    public static string Summarize(this Cellbits cell, bool emoji = false)
    {
        string? lbl = TryGetLabel(cell, out byte b) ? $"({b})" : null;
        IEnumerable<string?> tokens = emoji ? [
         cell==Cellbits.NOTHING ? "🆓" : "",
         cell.HasFlag(Cellbits.BLOCKED) ? "⛔" : "",
         cell.HasFlag(Cellbits.ROOM) ? "®" : "",
         cell.HasFlag(Cellbits.CORRIDOR) ? "⇴" : "",

         cell.HasFlag(Cellbits.DOORSPACE) ? "🚪" : "",
         cell.HasFlag(Cellbits.STAIRS) ? "🧗‍♂️" : "",
         cell.HasFlag(Cellbits.PERIMETER) ? "□" : "",
         cell.HasFlag(Cellbits.ENTRANCE) ? "🔰" : "",
        ] : [
                     cell==Cellbits.NOTHING ? " " : "",
                 cell.HasAnyFlag(Cellbits.BLOCKED) ? "X" : "",
                 cell.HasAnyFlag(Cellbits.ROOM) ? "R" : "",
                 cell.HasAnyFlag(Cellbits.CORRIDOR) ? "c" : "",
                 cell.HasAnyFlag(Cellbits.DOORSPACE) ? "D" : "",
                 cell.HasAnyFlag(Cellbits.STAIRS)? "S" : "",
                 cell.HasFlag(Cellbits.PERIMETER) ? "p" : "",
                 cell.HasFlag(Cellbits.ENTRANCE) ? "e" : "",
        ];
        return string.Concat(tokens);
    }

    [Obsolete(nameof(DataField<>))]
    public static Cellbits At(this Cellbits[,] self, (int, int) coord) => self[coord.Item1, coord.Item2];
    [Obsolete(nameof(DataField<>))]
    public static Cellbits Get(this Cellbits[,] self, SixLabors.ImageSharp.Point coord) => self[coord.Row(), coord.Col()];
}
