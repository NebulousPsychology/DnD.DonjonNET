// Adapted from https://donjon.bin.sh/code/dungeon/dungeon.pl
// https://creativecommons.org/licenses/by-nc/3.0/
namespace Donjon.Original;
#pragma warning disable IDE1006 // Naming Styles

public struct Sill
{
    public int door_r; public int door_c;
    public int sill_r; public int sill_c;
    public int? out_id; public Cardinal dir;
}
#pragma warning restore IDE1006 // Naming Styles
