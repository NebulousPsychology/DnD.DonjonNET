// Adapted from https://donjon.bin.sh/code/dungeon/dungeon.pl
// https://creativecommons.org/licenses/by-nc/3.0/
using System.Diagnostics;

namespace Donjon;
#pragma warning disable IDE1006 // Naming Styles

[DebuggerDisplay("@{Coord}-{type}-{out_id}")]
public struct DoorData
{
    public int row;
    public int col;
    public string key;
    public string type;
    public int? out_id;
    public string? desc;
    public readonly (int r, int c) Coord => (row, col);
}

#pragma warning restore IDE1006 // Naming Styles
