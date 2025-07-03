// Adapted from https://donjon.bin.sh/code/dungeon/dungeon.pl
// https://creativecommons.org/licenses/by-nc/3.0/
using System.Diagnostics;

namespace Donjon.Original;
#pragma warning disable IDE1006 // Naming Styles

[Flags]
public enum Cellbits : UInt32
{
    NOTHING = 0x0000_0000,
    BLOCKED = 0x0000_0001,  // 0001
    ROOM = 0x0000_0002,     // 0010
    CORRIDOR = 0x0000_0004, // 0100

    [Obsolete("0x08 is unpsecified")] Unspecified = 0x0000_0008,
    PERIMETER = 0x0000_0010,// 0000_0000_0001_0000
    ENTRANCE = 0x0000_0020, // 0000_0000_0010_0000
    ROOM_ID = 0x0000_FFC0,  // 1111_1111_1100_0000

    DOOR_ARCH = 0x0001_0000,
    /// <summary>a *plain* door, not *any* door, use DOORSPACE for that</summary>
    DOOR_SIMPLE = 0x0002_0000,
    DOOR_LOCKED = 0x0004_0000,
    DOOR_TRAPPED = 0x0008_0000,
    DOOR_SECRET = 0x0010_0000,
    DOOR_PORTC = 0x0020_0000,

    STAIR_DN = 0x0040_0000,
    STAIR_UP = 0x0080_0000,

    LABEL = 0xFF00_0000, // 1111_0000_0001_0000

    OPENSPACE = ROOM | CORRIDOR,
    DOORSPACE = DOOR_ARCH | DOOR_SIMPLE | DOOR_LOCKED | DOOR_TRAPPED | DOOR_SECRET | DOOR_PORTC,
    STAIRS = STAIR_DN | STAIR_UP,
    ESPACE = ENTRANCE | DOORSPACE | 0xFF00_0000,

    BLOCK_ROOM = BLOCKED | ROOM,
    BLOCK_CORR = BLOCKED | PERIMETER | CORRIDOR,
    BLOCK_DOOR = BLOCKED | DOORSPACE,
    LABELSPACE = LABEL | ROOM_ID
}

#pragma warning restore IDE1006 // Naming Styles
