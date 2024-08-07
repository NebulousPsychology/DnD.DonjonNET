# DonjonNET

Donjon's dungeon generator, translated to C#

## The Original

- source: <https://donjon.bin.sh/code/dungeon/dungeon.pl>
- license: <https://creativecommons.org/licenses/by-nc/3.0/>

### Neighbor projects

- <https://github.com/barraudf/Unity-RandomDungeonGenerator> GPL-2.0
  - DonjonNET differentiates as a standalone classlib, not coupled to Unity.
- <https://github.com/krmaxwell/donjon> (10yr+ old collection of generators)
  - random/generator.js is CC0 1.0 (public domain)
  - Fractal worldmap generator is GPL-2.0+

## Features on donjon.bin.sh but not in the perl foundation

TODO: Before implementing dungeon furnishings/loot/monsters from DMG, confirm that content is SRD-ok
<https://www.5esrd.com/gamemastering/engineering-dungeons/>

- dungeon name
- monsters, treasure and furnishings based on `party size`, `party level`, `dungeon motif`
- freestanding room traps
- trap details

- peripheral egress settings `no, yes, many, tile`
- preset dungeon sizes (all custom here by design)
- any `dungeon_layout` other than `box,cross` (8 others)
- other `roomLayout`s `sparse,symmetric`
- preset room sizes (all custom here by design)
- door settings (none,basic,secure,standard,deathtrap)
- history
- walls, floor, temperature, illumination
- corridor features
