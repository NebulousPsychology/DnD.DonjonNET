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
    /// <summary>
    /// arch, open, lock, trap, secret, portc
    /// </summary>
    public string key;
    /// <summary>
    /// archway, unlocked door, etc.
    /// </summary>
    public string type;
    public int? out_id;
    public string? desc;
    public Cardinal open_dir;
    public readonly (int r, int c) Coord => (row, col);
    public readonly bool IsSecret => Enumerable
        .Contains([ "trap", "secret"], key);
}

#pragma warning restore IDE1006 // Naming Styles
