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

## Intent

This project was undertaken as an exercise in design patterns. Over-engineering of components is likely, if not deliberate (consciously noting that it obviously is not best practice in itself).

- This project should support exported json schema at-parity, well enough to be usable by tools which include Donjon importers such as [Dungeon Scrawl](https://app.dungeonscrawl.com/)
- This project may grow to introduce fine-grained edit and re-generation actions, provided a straight end-to-end execution remains at-parity with the original implementation once translated to dotnet.
  - parity between standard randomness of perl and dotnet is not assured, so parity check tests shall be against the initial verbatim-translation for a given seed.
