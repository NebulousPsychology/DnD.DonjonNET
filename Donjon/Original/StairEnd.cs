// Adapted from https://donjon.bin.sh/code/dungeon/dungeon.pl
// https://creativecommons.org/licenses/by-nc/3.0/
namespace Donjon.Original;
#pragma warning disable IDE1006 // Naming Styles

[System.Diagnostics.DebuggerDisplay("'{key}' ({row},{col}) -> ({next_row},{next_col})")]
public record StairEnd
{
    public int row; public int col;
    public string? key;
    public int next_row; public int next_col;
}

#pragma warning restore IDE1006 // Naming Styles
