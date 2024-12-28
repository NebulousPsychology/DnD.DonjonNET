using System.Text.Json;

using Donjon.Original;

using Microsoft.Extensions.Logging;

using SixLabors.ImageSharp;
namespace Donjon;

public partial class DungeonGenRefactored
{

    #region place rooms
    /// <summary>place rooms, according to the chosen layout strategy</summary>
    /// <remarks><code>
    /// # emplace rooms
    /// 
    /// sub emplace_rooms {
    ///   my ($dungeon) = @_;
    /// 
    ///   if ($dungeon->{'room_layout'} eq 'Packed') {
    ///     $dungeon = &pack_rooms($dungeon);
    ///   } else {
    ///     $dungeon = &scatter_rooms($dungeon);
    ///   }
    ///   return $dungeon;
    /// }
    /// </code>
    /// </remarks>
    Dungeon emplace_rooms(Dungeon dungeon)
    {
        using (logger.BeginScope(nameof(emplace_room)))
        {
            dungeon = dungeon.room_layout switch
            {
                RoomLayout.Packed => pack_rooms(dungeon),
                RoomLayout.Scattered => scatter_rooms(dungeon),
                _ => throw new Exception($"Unrecognized case {dungeon.room_layout}"),
            };
            logger.LogInformation("Emplaced {n} rooms", dungeon.room.Count);
            return dungeon;
        }
    }


    /// <summary>room placement strategy, using "Pack": step2 across the dungeon, if a cell is not already a room</summary>
    /// <remarks><code>
    /// sub pack_rooms {
    ///   my ($dungeon) = @_;
    ///   my $cell = $dungeon->{'cell'};
    /// 
    ///   my $i; for ($i = 0; $i < $dungeon->{'n_i'}; $i++) {
    ///       my $r = ($i * 2) + 1;
    ///     my $j; for ($j = 0; $j < $dungeon->{'n_j'}; $j++) {
    ///       my $c = ($j * 2) + 1;
    /// 
    ///       next if ($cell->[$r][$c] & $ROOM);
    ///       next if (($i == 0 || $j == 0) && int(rand(2)));
    /// 
    ///       my $proto = { 'i' => $i, 'j' => $j };
    ///       $dungeon = &emplace_room($dungeon,$proto);
    ///     }
    ///   }
    ///   return $dungeon;
    /// }
    /// </code></remarks>
    Dungeon pack_rooms(Dungeon dungeon)
    {
        using (logger.BeginScope(nameof(pack_rooms)))
        {
            //RASTER: HEMI: EXCLUSIVE <0,0>..<nrows/2,ncols/2>
            foreach (var (i, j) in Dim2d.RangeInclusive(0, dungeon.n_i - 1, 0, dungeon.n_j - 1))
            {
                var r = 2 * i + 1;
                var c = 2 * j + 1;
                //__ across the dungeon's hemicell space, ...

                //::       next if ($cell->[$r][$c] & $ROOM);
                if (dungeon.cell[r, c].HasFlag(Cellbits.ROOM)) continue;

                logger.LogInformation("pack-request: room {i} of {n}, last id={id}", i + 1, dungeon.n_rooms,
                      dungeon.last_room_id?.ToString() ?? "null");

                //::       next if (($i == 0 || $j == 0) && int(rand(2)));
                if ((i == 0 || j == 0) && dungeon.random.Next(2) != 0) continue;
                //? why single out 0th row and column for cointoss ?

                var proto = (i, j); // coordinates in hemicell space
                dungeon = emplace_room(dungeon, proto);
            }
            return dungeon;
        }
    }

    /// <summary>room placement strategy: just place the quota of rooms, with no prototups</summary>
    /// <remarks><code>
    /// sub scatter_rooms {
    ///   my ($dungeon) = @_;
    ///   my $n_rooms = &alloc_rooms($dungeon);
    /// 
    ///   my $i; for ($i = 0; $i < $n_rooms; $i++) {
    ///     $dungeon = &emplace_room($dungeon);
    ///   }
    ///   return $dungeon;
    /// }
    /// </code></remarks>
    Dungeon scatter_rooms(Dungeon dungeon)
    {
        using (logger.BeginScope(nameof(scatter_rooms)))
        {
            for (int i = 0; i < MyRoomIssuer.Maximum; i++)
            {
                logger.LogInformation("scatter-request: room {i} of {n}, last id={id}", i + 1, MyRoomIssuer.Maximum, MyRoomIssuer.last_room_id?.ToString() ?? "null");
                dungeon = emplace_room(dungeon, prototup: null);
            }
            return dungeon;
        }
    }

    /// <summary>
    /// Place a room
    /// </summary>
    /// <param name="dungeon">...</param>
    /// <param name="prototup"> i,j coordinates in hemicell space:: scatter:null, pack: hemicell;  used in set_room</param>
    /// <remarks><code>
    /// sub emplace_room {
    ///   my ($dungeon,$proto) = @_;
    ///      return $dungeon if ($dungeon->{'n_rooms'} == 999);
    ///   my ($r,$c);
    ///   my $cell = $dungeon->{'cell'};
    /// 
    ///   # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    ///   # room position and size
    /// 
    ///   $proto = &set_room($dungeon,$proto);
    /// 
    ///   # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    ///   # room boundaries
    /// 
    ///   my $r1 = ( $proto->{'i'}                       * 2) + 1;
    ///   my $c1 = ( $proto->{'j'}                       * 2) + 1;
    ///   my $r2 = (($proto->{'i'} + $proto->{'height'}) * 2) - 1;
    ///   my $c2 = (($proto->{'j'} + $proto->{'width'} ) * 2) - 1;
    /// 
    ///   return $dungeon if ($r1 < 1 || $r2 > $dungeon->{'max_row'});
    ///   return $dungeon if ($c1 < 1 || $c2 > $dungeon->{'max_col'});
    /// 
    ///   # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    ///   # check for collisions with existing rooms
    /// 
    /// emplace_room_collisiontest
    ///   # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    ///   # emplace room
    /// carve
    ///   # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    ///   blockperim
    ///   # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// 
    ///   return $dungeon;
    /// }
    /// </code></remarks>
    Dungeon emplace_room(Dungeon dungeon, (int i, int j)? prototup)
    {
        using (logger.BeginScope($"{nameof(emplace_room)} {prototup?.ToString() ?? "(randomized)"}"))
        {
            if (dungeon.n_rooms == 999) return dungeon;

            //RASTER: (prototup,if any, comes from a...) HEMI: EXCLUSIVE <0,0>..<nrows/2,ncols/2>
            // # room position and size
            var proto = set_room(dungeon, prototup);

            //   # room boundaries

            // get the room realspace extents from the hemispace rectangle
            //::   my $r1 = ( $proto->{'i'}                       * 2) + 1;
            //::   my $c1 = ( $proto->{'j'}                       * 2) + 1;
            //::   my $r2 = (($proto->{'i'} + $proto->{'height'}) * 2) - 1;
            //::   my $c2 = (($proto->{'j'} + $proto->{'width'} ) * 2) - 1;
            // TODO: set_room could carry the responsibility for converting back to realspace?
            Realspace<int> r1 = (proto["i"] * 2) + 1;
            Realspace<int> c1 = (proto["j"] * 2) + 1;
            Realspace<int> r2 = ((proto["i"] + proto["height"]) * 2) - 1;
            Realspace<int> c2 = ((proto["j"] + proto["width"]) * 2) - 1;

            // if any corner breaks the outermost border, eject
            if (r1 < 1 || r2 > dungeon.max_row) return dungeon;
            if (c1 < 1 || c2 > dungeon.max_col) return dungeon;

            if (false == emplace_room_collisiontest(dungeon, out int proposed_room_id, r1, c1, r2, c2)) return dungeon;

            // # emplace room
            emplace_room_carve(dungeon, r1, c1, r2, c2, proposed_room_id);

            //   # block corridors from room boundary
            //   # check for door openings from adjacent rooms
            emplace_room_BlockPerimeter(dungeon, r1, c1, r2, c2);

            return dungeon;
        }
    }

    /// <summary>
    /// create a room in the designated rectangle
    /// </summary>
    /// <remarks><code>
    ///   for ($r = $r1; $r <= $r2; $r++) {
    ///     for ($c = $c1; $c <= $c2; $c++) {
    ///       if ($cell->[$r][$c] & $ENTRANCE) {
    ///         $cell->[$r][$c] &= ~ $ESPACE;
    ///       } elsif ($cell->[$r][$c] & $PERIMETER) {
    ///         $cell->[$r][$c] &= ~ $PERIMETER;
    ///       }
    ///       $cell->[$r][$c] |= $ROOM | ($room_id << 6);
    ///     }
    ///   }
    ///   my $height = (($r2 - $r1) + 1) * 10;
    ///   my $width = (($c2 - $c1) + 1) * 10;
    /// 
    ///   my $room_data = {
    ///     'id' => $room_id, 'row' => $r1, 'col' => $c1,
    ///     'north' => $r1, 'south' => $r2, 'west' => $c1, 'east' => $c2,
    ///     'height' => $height, 'width' => $width, 'area' => ($height * $width)
    ///   };
    ///   $dungeon->{'room'}[$room_id] = $room_data;
    /// </code></remarks>
    private void emplace_room_carve(Dungeon dungeon, Realspace<Rectangle> rect, int proposed_room_id)
    {
        using (logger.BeginScope("mini:emplaceroom"))
        {
            //RASTER: INCLUSIVE <r1,c1>..<r2,c2>
            foreach (var (r, c) in Dim2d.RangeInclusive(rect))
            {
                // remove Entrance marker
                if (dungeon.cell[r, c].HasFlag(Cellbits.ENTRANCE)) //::       if ($cell->[$r][$c] & $ENTRANCE) {
                {
                    dungeon.cell[r, c] &= ~Cellbits.ESPACE;
                }
                else if (dungeon.cell[r, c].HasFlag(Cellbits.PERIMETER)) //::       } elsif ($cell->[$r][$c] & $PERIMETER) {
                {
                    // remove Perimiter marker
                    dungeon.cell[r, c] &= ~Cellbits.PERIMETER;
                }

                // Add room marker, plus the room Id:  make the cell a member of proposedRoomId
                //// dungeon.cell[r, c] |= Cellbits.ROOM | (Cellbits)(proposed_room_id << 6);
                dungeon.cell[r, c].TrySetRoomId(proposed_room_id);

            }

            int cellsize = 1; //! is an alteration vs perl: original specifies 10
            IDungeonRoom room_data = new DungeonRoomRectStruct
            {
                id = proposed_room_id,
                Rectangle = rect,
                // row = rect.Value.Y, // r1,
                // col = rect.Value.X, // c1,
                // north = rect.Value.Top, // r1,
                // south = rect.Value.Bottom - 1, // r2,
                // west = rect.Value.Left, // c1,
                // east = rect.Value.Right - 1, // c2,
                // height = rect.Value.Height, // ((r2 - r1) + 1) * cellsize,
                // width = rect.Value.Width, // ((c2 - c1) + 1) * cellsize,
                door = [],
            };
            dungeon.room[proposed_room_id] = room_data;
            logger.LogInformation(message: "AddRoom: {r}", JsonSerializer.Serialize(room_data, jsonLoggingOptions));
        }
    }

    [Obsolete("Prefer Realspace<Rectangle>")]
    private void emplace_room_carve(Dungeon dungeon, int r1, int c1, int r2, int c2, int proposed_room_id)
        => emplace_room_carve(dungeon, new Realspace<Rectangle>(new(x: c1, y: r1, width: c2 - c1, height: r2 - r1)), proposed_room_id);


    /// <summary>
    /// 
    /// </summary>
    /// <param name="dungeon">context</param>
    /// <param name="proposed_room_id">room id, if a new room gets issued</param>
    /// <param name="r">a realspace region, enclosing the coordinates of the proposed room</param>
    /// <returns>true if a new room was issued</returns>
    /// <remarks><code>
    ///   my $hit = &sound_room($dungeon,$r1,$c1,$r2,$c2);
    ///      return $dungeon if ($hit->{'blocked'});
    ///   my @hit_list = keys %{ $hit };
    ///   my $n_hits = scalar @hit_list;
    ///   my $room_id;
    /// 
    ///   if ($n_hits == 0) {
    ///     $room_id = $dungeon->{'n_rooms'} + 1;
    ///     $dungeon->{'n_rooms'} = $room_id;
    ///   } else {
    ///     return $dungeon;
    ///   }
    ///   $dungeon->{'last_room_id'} = $room_id;
    /// </code></remarks>
    bool emplace_room_collisiontest(IDungeon dungeon, out int proposed_room_id, Realspace<Rectangle> rect)
    {
        //RASTER: (coords,if any, comes from a...) HEMI: EXCLUSIVE <0,0>..<nrows/2,ncols/2>
        //   # check for collisions with existing rooms
        using (logger.BeginScope("mini:collisiontest"))
        {
            if (TrySoundRoom(dungeon, rect, out var block, out var hit))
            {
                logger.LogDebug("Room {r} @[{extents}]: approved because no hits", MyRoomIssuer.Current, rect.ToString());
                return MyRoomIssuer.TryIssueRoom(out proposed_room_id) ? true : throw new Exception($"Ran out of rooms? {MyRoomIssuer.Current}");
            }
            else if (block)
            {
                logger.LogTrace("sounding resulted in block");
                proposed_room_id = -1;
                return false;
            }
            else
            {
                logger.LogInformation("Room {r} @[{extents}]: rejected because hits={h} > 0 ", MyRoomIssuer.Current, rect.ToString(), hit.Count);
                proposed_room_id = -1;
                return false;
            }
        }
    }

    [Obsolete("Prefer Rectangles")]
    bool emplace_room_collisiontest(IDungeon dungeon, out int proposed_room_id, int r1, int c1, int r2, int c2)
            => emplace_room_collisiontest(dungeon, out proposed_room_id, new(new(c1, r1, c2 - c1, r2 - r1)));

    /// <summary>
    /// block corridors from room boundary; check for door openings from adjacent rooms
    /// </summary>
    /// <param name="dungeon"></param>
    /// <remarks><code>
    ///   for ($r = $r1 - 1; $r <= $r2 + 1; $r++) {
    ///     unless ($cell->[$r][$c1 - 1] & ($ROOM | $ENTRANCE)) {
    ///       $cell->[$r][$c1 - 1] |= $PERIMETER;
    ///     }
    ///     unless ($cell->[$r][$c2 + 1] & ($ROOM | $ENTRANCE)) {
    ///       $cell->[$r][$c2 + 1] |= $PERIMETER;
    ///     }
    ///   }
    ///   for ($c = $c1 - 1; $c <= $c2 + 1; $c++) {
    ///     unless ($cell->[$r1 - 1][$c] & ($ROOM | $ENTRANCE)) {
    ///       $cell->[$r1 - 1][$c] |= $PERIMETER;
    ///     }
    ///     unless ($cell->[$r2 + 1][$c] & ($ROOM | $ENTRANCE)) {
    ///       $cell->[$r2 + 1][$c] |= $PERIMETER;
    ///     }
    ///   }
    /// </code></remarks>
    void emplace_room_BlockPerimeter(IDungeon dungeon, Realspace<int> r1, Realspace<int> c1, Realspace<int> r2, Realspace<int> c2)
    {
        void PerimeterizeIfNonroomNonentrance(int r, int c)
        {
            if (!dungeon.cell[r, c].HasAnyFlag(Cellbits.ROOM | Cellbits.ENTRANCE)) //! or-entrance is NOT an alteration vs perl!
            {
                dungeon.cell[r, c] |= Cellbits.PERIMETER;
            }
        }
        using (logger.BeginScope("mini:block room bound"))
        {
            //RASTER: REALSPACE: INCLUSIVE INFLATED <r1-1,c1-1>..<r2+1,c2+1>
            for (int r = r1 - 1; r <= r2 + 1; r++) // note: 1-cell outset
            {
                PerimeterizeIfNonroomNonentrance(r, c1 - 1);
                PerimeterizeIfNonroomNonentrance(r, c2 + 1);
            }

            for (int c = c1 - 1; c <= c2 + 1; c++)
            {
                PerimeterizeIfNonroomNonentrance(r1 - 1, c);
                PerimeterizeIfNonroomNonentrance(r2 + 1, c);
            }
        }// /scope
    }

    /// <remarks><code>
    /// sub set_room {
    ///   my ($dungeon,$proto) = @_;
    ///   my $base = $dungeon->{'room_base'};
    ///   my $radix = $dungeon->{'room_radix'};
    /// 
    ///   unless (defined $proto->{'height'}) {
    ///     if (defined $proto->{'i'}) {
    ///       my $a = $dungeon->{'n_i'} - $base - $proto->{'i'};
    ///          $a = 0 if ($a < 0);
    ///       my $r = ($a < $radix) ? $a : $radix;
    /// 
    ///       $proto->{'height'} = int(rand($r)) + $base;
    ///     } else {
    ///       $proto->{'height'} = int(rand($radix)) + $base;
    ///     }
    ///   }
    ///   unless (defined $proto->{'width'}) {
    ///     if (defined $proto->{'j'}) {
    ///       my $a = $dungeon->{'n_j'} - $base - $proto->{'j'};
    ///          $a = 0 if ($a < 0);
    ///       my $r = ($a < $radix) ? $a : $radix;
    /// 
    ///       $proto->{'width'} = int(rand($r)) + $base;
    ///     } else {
    ///       $proto->{'width'} = int(rand($radix)) + $base;
    ///     }
    ///   }
    ///   unless (defined $proto->{'i'}) {
    ///     $proto->{'i'} = int(rand($dungeon->{'n_i'} - $proto->{'height'}));
    ///   }
    ///   unless (defined $proto->{'j'}) {
    ///     $proto->{'j'} = int(rand($dungeon->{'n_j'} - $proto->{'width'}));
    ///   }
    ///   return $proto;
    /// }
    /// </code></remarks>
    /// <summary>room position and size</summary>
    /// <returns>
    ///     a dictionary [i,j,height,width] representing a rectangle in hemispace:
    ///     where i&j are either the original hemispace coords OR a random hemispace coord that can fit the room
    ///     FIXME: uncertainty remains on whether this directly maps to Rectangle types
    /// </returns>
    [Obsolete("Prefer Hemispace<Point>")]
    IDictionary<string, int> set_room(Dungeon dungeon, (int i, int j)? prototuple)
    {
        using (logger.BeginScope(nameof(set_room)))
        {
            var roomBase = dungeon.room_base;//? even, if room_minmax are obligate odd
            var roomRadix = dungeon.room_radix; //? odd, if room_minmax are obligate odd
            int createRadix(int hemicelladdress, int hemicellmax)
                => Math.Min(roomRadix, Math.Max(0, hemicellmax - roomBase - hemicelladdress));
#if Original
            Dictionary<string, int> proto = prototuple is null ? [] : new()
            {
                ["i"] = prototuple.Value.i,
                ["j"] = prototuple.Value.j,
            };

            //::   unless (defined $proto->{'height'}) {
            if (!proto.ContainsKey("height"))
            {
                var rdx = prototuple.HasValue ? fromprototup(prototuple.Value.i, dungeon.n_i) : roomRadix;
                proto.Add("height", dungeon.random.Next(rdx) + roomBase);
            }
            //::   unless (defined $proto->{'width'}) {
            if (!proto.ContainsKey("width"))
            {
                var radx = prototuple.HasValue ? fromprototup(prototuple.Value.j, dungeon.n_j) : roomRadix;
                proto.Add("width", dungeon.random.Next(radx) + roomBase);
            }
            //::   unless (defined $proto->{'i'}) {
            //::     $proto->{'i'} = int(rand($dungeon->{'n_i'} - $proto->{'height'}));
            //::   }
            _ = proto.TryAdd("i", dungeon.random.Next(dungeon.n_i - proto["height"]));
            //::   unless (defined $proto->{'j'}) {
            //::     $proto->{'j'} = int(rand($dungeon->{'n_j'} - $proto->{'width'}));
            //::   }
            _ = proto.TryAdd("j", dungeon.random.Next(dungeon.n_j - proto["width"]));
            return proto;
#else
            //RASTER: (prototup,if any, comes from a...) HEMI: EXCLUSIVE <0,0>..<ni=nrows/2,nj=ncols/2>
            Dictionary<string, int> proto = prototuple is null ? new()
            {
                ["height"] = dungeon.random.Next(roomRadix) + roomBase,
                ["width"] = dungeon.random.Next(roomRadix) + roomBase,
            } : new()
            {
                ["i"] = prototuple.Value.i,
                ["j"] = prototuple.Value.j,
                ["height"] = dungeon.random.Next(createRadix(prototuple.Value.i, dungeon.n_i)) + roomBase,
                ["width"] = dungeon.random.Next(createRadix(prototuple.Value.j, dungeon.n_j)) + roomBase,
            };

            //__ find a hemispace coordinate that can fit a room of the chosen size
            _ = proto.TryAdd("i", dungeon.random.Next(dungeon.n_i - proto["height"]));
            _ = proto.TryAdd("j", dungeon.random.Next(dungeon.n_j - proto["width"]));

            return proto;
#endif
        }
    }

    /// <summary>propose room position and size</summary>
    /// <returns>
    ///     a dictionary [i,j,height,width] representing a rectangle in hemispace:
    ///     where i&j are either the original hemispace coords OR a random hemispace coord that can fit the room
    ///     FIXME: uncertainty remains on whether this directly maps to Rectangle types
    /// </returns>
    private Hemispace<Rectangle> set_room(Dungeon dungeon, Hemispace<Point>? prototuple)
    {
        //RASTER: (prototup,if any, comes from a...) HEMI: EXCLUSIVE <0,0>..<ni=nrows/2,nj=ncols/2>
        using (logger.BeginScope(nameof(set_room)))
        {
            var roomBase = dungeon.room_base;//? even, if room_minmax are obligate odd
            var roomRadix = dungeon.room_radix; //? odd, if room_minmax are obligate odd
            int createRadix(int hemicelladdress, int hemicellmax)
                => Math.Min(roomRadix, Math.Max(0, hemicellmax - roomBase - hemicelladdress));

            var radix = prototuple is Hemispace<Point> rpt ? (
                    i: createRadix(rpt.Value.Y, dungeon.n_i),
                    j: createRadix(rpt.Value.X, dungeon.n_j)
                )
                : (i: roomRadix, j: roomRadix);

            Size size = new(width: dungeon.random.Next(radix.j) + roomBase,
                            height: dungeon.random.Next(radix.i) + roomBase);

            Point p = prototuple is Hemispace<Point> point ? point.Value
                : new Point(x: dungeon.random.Next(dungeon.n_j - size.Width),
                            y: dungeon.random.Next(dungeon.n_i - size.Height));

            return new Hemispace<Rectangle>(new Rectangle(p, size));
        }
    }

    /// <remarks><code>
    /// sub sound_room {
    ///   my ($dungeon,$r1,$c1,$r2,$c2) = @_;
    ///   my $cell = $dungeon->{'cell'};
    ///   my $hit;
    /// 
    ///   my $r; for ($r = $r1; $r <= $r2; $r++) {
    ///     my $c; for ($c = $c1; $c <= $c2; $c++) {
    ///       if ($cell->[$r][$c] & $BLOCKED) {
    ///         return { 'blocked' => 1 };
    ///       }
    ///       if ($cell->[$r][$c] & $ROOM) {
    ///         my $id = ($cell->[$r][$c] & $ROOM_ID) >> 6;
    ///         $hit->{$id} += 1;
    ///       }
    ///     }
    ///   }
    ///   return $hit;
    /// }
    /// </code></remarks>
    /// <summary>test cells in the inclusive region. 
    /// if any are blocked, set a return flag. 
    /// if any belong to a designated room, produce a count of how many per room</summary>
    /// <remarks>
    /// {
    /// blocked=1
    /// }
    /// </remarks>
    Dictionary<string, int> sound_room(IDungeon dungeon, Realspace<Rectangle> rect)
    {
        using (logger.BeginScope(nameof(sound_room)))
        {
            Dictionary<string, int> hit = [];

            //RASTER: REALSPACE: INCLUSIVE <r1,c1>..<r2,c2>
            foreach (var (r, c) in Dim2d.RangeInclusive(rect))
            {
                if (dungeon.cell[r, c].HasFlag(Cellbits.BLOCKED))
                {
                    return new Dictionary<string, int> { ["blocked"] = 1 };
                }
                if (dungeon.cell[r, c].TryGetRoomId(out int roomid))
                {
                    if (hit.TryGetValue(roomid.ToString(), out int prevhitcount))
                    {
                        hit[roomid.ToString()] = prevhitcount + 1;
                    }
                    else
                    {
                        hit.Add(roomid.ToString(), 1);
                    }
                }
            }
            logger.LogDebug("sounding {rect}: {hits}", rect.ToJson(), string.Join(",", hit.Select(kvp => $"'{kvp.Key}'={kvp.Value}")));
            return hit;
        }
    }
    [Obsolete("Prefer Rectangles")]
    Dictionary<string, int> sound_room(IDungeon dungeon, int r1, int c1, int r2, int c2)
        => sound_room(dungeon, new(new(c1, r1, c2 - c1, r2 - r1)));

    bool TrySoundRoom(IDungeon dungeon, Realspace<Rectangle> rect, out bool blocked, out Dictionary<string, int> hit)
    {
        hit = sound_room(dungeon, rect);
        blocked = hit.ContainsKey("blocked");
        return hit is { Count: 0 };
    }
    #endregion place rooms

}
