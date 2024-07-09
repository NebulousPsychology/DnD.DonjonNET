// Adapted from https://donjon.bin.sh/code/dungeon/dungeon.pl
// https://creativecommons.org/licenses/by-nc/3.0/
namespace Donjon;
#pragma warning disable IDE1006 // Naming Styles

public interface IDungeonRoom
{
    public int id { get; }
    public int north { get; }
    public int south { get; }
    public int east { get; }
    public int west { get; }
    public int row { get; }
    public int col { get; }
    public int height { get; }
    public int width { get; }
    public int area => height * width;
    public int Perimeter => 2 * (height + width);
    public Dictionary<Cardinal, List<DoorData>> door { get; }
}
#pragma warning restore IDE1006 // Naming Styles
