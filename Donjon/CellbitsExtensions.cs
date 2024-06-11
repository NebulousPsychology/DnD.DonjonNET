// Adapted from https://donjon.bin.sh/code/dungeon/dungeon.pl
// https://creativecommons.org/licenses/by-nc/3.0/
namespace Donjon;

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
    public static Cellbits SetLabel(this Cellbits cell, char c) => cell | (Cellbits)(c << 24);
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
}
