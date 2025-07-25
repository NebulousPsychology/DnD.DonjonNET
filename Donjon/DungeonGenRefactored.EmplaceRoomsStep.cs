using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Donjon.Original;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SixLabors.ImageSharp;
namespace Donjon;

public partial class DungeonGenRefactored
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="settings"></param>
    /// <param name="random"></param>
    /// <param name="MyRoomIssuer"></param>
    public class EmplaceRoomsStep(
        ILogger<EmplaceRoomsStep> logger,
        IOptions<Settings> settings,
        Random random,
        RoomIdIssuer MyRoomIssuer
        )
    : Pipeline.IPipelineOperation<IDungeon>
    {
        public bool TryInvoke(IDungeon input, [MaybeNullWhen(false), NotNullWhen(true)] out IDungeon? result)
        {
            result = emplace_rooms(input);
            return true;
        }

        #region place rooms
        /// <summary>place rooms, according to the chosen layout strategy</summary>
        IDungeon emplace_rooms(IDungeon dungeon)
        {
            /*
            //:: sub emplace_rooms {
            //::   my ($dungeon) = @_;
            //:: 
            //::   if ($dungeon->{'room_layout'} eq 'Packed') {
            //::     $dungeon = &pack_rooms($dungeon);
            //::   } else {
            //::     $dungeon = &scatter_rooms($dungeon);
            //::   }
            //::   return $dungeon;
            //:: }
            */
            using (logger.BeginScope(nameof(emplace_rooms)))
            {
                dungeon = settings.Value.Rooms.room_layout switch
                {
                    RoomLayout.Packed => pack_rooms(dungeon),
                    RoomLayout.Scattered => scatter_rooms(dungeon),
                    _ => throw new Exception($"Unrecognized case {settings.Value.Rooms.room_layout}"),
                };
                logger.LogInformation("Emplaced {n} rooms", dungeon.room.Count);
                return dungeon;
            }
        }


        /// <summary>room placement strategy, using "Pack": step2 across the dungeon, if a cell is not already a room</summary>
        /// <remarks>uses <see cref="ni*nj"/> randoms</remarks>
        IDungeon pack_rooms(IDungeon dungeon)
        {
            /*
            //:: sub pack_rooms {
            //::   my ($dungeon) = @_;
            //::   my $cell = $dungeon->{'cell'};
            //:: 
            //::   my $i; for ($i = 0; $i < $dungeon->{'n_i'}; $i++) {
            //::       my $r = ($i * 2) + 1;
            //::     my $j; for ($j = 0; $j < $dungeon->{'n_j'}; $j++) {
            //::       my $c = ($j * 2) + 1;
            //:: 
            //::       next if ($cell->[$r][$c] & $ROOM);
            //::       next if (($i == 0 || $j == 0) && int(rand(2)));
            //:: 
            //::       my $proto = { 'i' => $i, 'j' => $j };
            //::       $dungeon = &emplace_room($dungeon,$proto);
            //::     }
            //::   }
            //::   return $dungeon;
            //:: }
            */
            using (logger.BeginScope(nameof(pack_rooms)))
            {
                //RASTER: HEMI: EXCLUSIVE <0,0>..<nrows/2,ncols/2>
                foreach (Hemispace<Coord> ij in Dim2d.RangeInclusive(0, dungeon.n_i - 1, 0, dungeon.n_j - 1).AsHemi())
                {
                    Realspace<Coord> rc = ij.ToRealspace();

                    // Skip cells that are already ROOM
                    //::       next if ($cell->[$r][$c] & $ROOM);
                    if (dungeon.cell.Get(rc).HasFlag(Cellbits.ROOM)) continue;

                    // When considering first hemispace row and column, skip by cointoss
                    //? why single out 0th row and column for cointoss ?
                    //::       next if (($i == 0 || $j == 0) && int(rand(2)));
                    if ((ij.Value.r == 0 || ij.Value.c == 0) && random.Next(2) != 0) continue;

                    logger.LogInformation("pack_rooms ready to place: {info}", new
                    {
                        room_proto = ij,
                        room_quota = dungeon.n_rooms,
                        last_room_id = dungeon.last_room_id?.ToString() ?? "null"
                    });
                    dungeon = emplace_room(dungeon, ij);
                }
                return dungeon;
            }
        }

        /// <summary>room placement strategy: just place the quota of rooms, with no prototups</summary>
        IDungeon scatter_rooms(IDungeon dungeon)
        {
            /*
            //:: sub scatter_rooms {
            //::   my ($dungeon) = @_;
            //::   my $n_rooms = &alloc_rooms($dungeon);
            //:: 
            //::   my $i; for ($i = 0; $i < $n_rooms; $i++) {
            //::     $dungeon = &emplace_room($dungeon);
            //::   }
            //::   return $dungeon;
            //:: }
            */
            using (logger.BeginScope(nameof(scatter_rooms)))
            {
                for (int i = 0; i < MyRoomIssuer.Maximum; i++)
                {
                    logger.LogInformation("scatter-request: {info}", new
                    {
                        Room = i + 1,
                        Room_Max = MyRoomIssuer.Maximum,
                        LastId = MyRoomIssuer.last_room_id?.ToString() ?? "null"
                    });

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
        public IDungeon emplace_room(IDungeon dungeon, Hemispace<Coord>? prototup)
        {
            /*
            //:: sub emplace_room {
            //::   my ($dungeon,$proto) = @_;
            //::      return $dungeon if ($dungeon->{'n_rooms'} == 999);
            //::   my ($r,$c);
            //::   my $cell = $dungeon->{'cell'};
            //:: 
            //::   # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
            //::   # room position and size
            //:: 
            //::   $proto = &set_room($dungeon,$proto);
            //:: 
            //::   # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
            //::   # room boundaries
            //:: 
            //::   my $r1 = ( $proto->{'i'}                       * 2) + 1;
            //::   my $c1 = ( $proto->{'j'}                       * 2) + 1;
            //::   my $r2 = (($proto->{'i'} + $proto->{'height'}) * 2) - 1;
            //::   my $c2 = (($proto->{'j'} + $proto->{'width'} ) * 2) - 1;
            //:: 
            //::   return $dungeon if ($r1 < 1 || $r2 > $dungeon->{'max_row'});
            //::   return $dungeon if ($c1 < 1 || $c2 > $dungeon->{'max_col'});
            //:: 
            //::   # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
            //::   # check for collisions with existing rooms
            //:: 
            //:: emplace_room_collisiontest
            //::   # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
            //::   # emplace room
            //:: carve
            //::   # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
            //::   blockperim
            //::   # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
            //:: 
            //::   return $dungeon;
            //:: }
            */
            using (logger.BeginScope($"{nameof(emplace_room)} {prototup?.ToString() ?? "(randomized)"}"))
            {
                if (dungeon.n_rooms == 999) return dungeon;

                //RASTER: (prototup,if any, comes from a...) HEMI: EXCLUSIVE <0,0>..<nrows/2,ncols/2>
                // # room position and size
                Hemispace<Rectangle> proto = set_room(dungeon, prototup);

                // Room boundaries: convert hemispace rectangle to realspace extents
                Realspace<Rectangle> rect = proto.ToRealspace();

                // Reject if any corner is outside the dungeon bounds 
                if (!dungeon.Contains(rect)) return dungeon;

                // Collision test: only proceed if the region is clear
                if (!emplace_room_collisiontest(dungeon, out int? proposed_room_id, rect))
                {
                    logger.LogWarning("emplace_room_collisiontest shortcircuit");
                    return dungeon;
                }

                // Carve the room into the dungeon grid
                emplace_room_carve(dungeon, proposed_room_id.Value, rect);

                // Block corridors from the room boundary and check for door openings from adjacent rooms
                emplace_room_BlockPerimeter(dungeon, rect);

                return dungeon;
            }
        }

        /// <summary>
        /// create a room in the designated rectangle
        /// </summary>
        /// <param name="dungeon"></param>
        /// <param name="rect"></param>
        /// <param name="proposed_room_id"></param>
        public void emplace_room_carve(IDungeon dungeon, int proposed_room_id, Realspace<Rectangle> rect)
        {
            /*
            //::   for ($r = $r1; $r <= $r2; $r++) {
            //::     for ($c = $c1; $c <= $c2; $c++) {
            //::       if ($cell->[$r][$c] & $ENTRANCE) {
            //::         $cell->[$r][$c] &= ~ $ESPACE;
            //::       } elsif ($cell->[$r][$c] & $PERIMETER) {
            //::         $cell->[$r][$c] &= ~ $PERIMETER;
            //::       }
            //::       $cell->[$r][$c] |= $ROOM | ($room_id << 6);
            //::     }
            //::   }
            //::   my $height = (($r2 - $r1) + 1) * 10;
            //::   my $width = (($c2 - $c1) + 1) * 10;
            //:: 
            //::   my $room_data = {
            //::     'id' => $room_id, 'row' => $r1, 'col' => $c1,
            //::     'north' => $r1, 'south' => $r2, 'west' => $c1, 'east' => $c2,
            //::     'height' => $height, 'width' => $width, 'area' => ($height * $width)
            //::   };
            //::   $dungeon->{'room'}[$room_id] = $room_data;
            */
            using (logger.BeginScope("{} @ {}", nameof(emplace_room_carve), rect))
            {
                //RASTER: INCLUSIVE <r1,c1>..<r2,c2>
                foreach (var (r, c) in Dim2d.RangeUpperExclusive(rect).AsRealspace())
                {
                    // remove Entrance/Door marker
                    if (dungeon.cell.SetIf((r, c), p => p.HasFlag(Cellbits.ENTRANCE), p => p & ~Cellbits.ESPACE) is false)
                        dungeon.cell.SetIf((r, c), p => p.HasFlag(Cellbits.PERIMETER), p => p & ~Cellbits.PERIMETER);

                    // Add room marker, plus the room Id:  make the cell a member of proposedRoomId
                    dungeon.cell[r, c].TrySetRoomId(proposed_room_id);
                }
                var roomcountbefore = dungeon.room.Count;
                // int cellsize = 1; //! is an alteration vs perl: original specifies 10
                IDungeonRoom room_data = new DungeonRoomRectStruct
                {
                    id = proposed_room_id,
                    Rectangle = rect,
                    door = [],
                };
                if (!dungeon.room.TryAdd(proposed_room_id, room_data))
                {
                    logger.LogWarning("Failed to add {id}. room count is {c}", proposed_room_id, dungeon.room.Count);
                }
                else
                {
                    logger.LogInformation(message: "AddRoom OK:@{} {r}", new { id = proposed_room_id, dungeon.room.Count }, room_data);
                }
                Debug.Assert(roomcountbefore < dungeon.room.Count);
            }
        }

        [Obsolete("Prefer Realspace<Rectangle>", error: true)]
        public void emplace_room_carve(IDungeon dungeon, int proposed_room_id,
            Realspace<int> r1, Realspace<int> c1, Realspace<int> r2, Realspace<int> c2)
            => emplace_room_carve(dungeon,
                proposed_room_id,
                new Realspace<Rectangle>(new()
                {
                    X = c1,
                    Y = r1,
                    Width = c2 - c1 + 1,
                    Height = r2 - r1 + 1
                }));


        /// <summary>
        /// test the room location, and issue a room id if viable
        /// </summary>
        /// <param name="dungeon">context</param>
        /// <param name="proposed_room_id">room id, if a new room gets issued</param>
        /// <param name="r">a realspace region, enclosing the coordinates of the proposed room</param>
        /// <returns>true if a new room was issued</returns>
        /// <remarks><code>
        /// </code></remarks>
        public bool emplace_room_collisiontest(IDungeon dungeon, [MaybeNullWhen(false), NotNullWhen(true)] out int? proposed_room_id, Realspace<Rectangle> rect)
        {
            /*
            //::   my $hit = &sound_room($dungeon,$r1,$c1,$r2,$c2);
            //::      return $dungeon if ($hit->{'blocked'});
            //::   my @hit_list = keys %{ $hit };
            //::   my $n_hits = scalar @hit_list;
            //::   my $room_id;
            //:: 
            //::   if ($n_hits == 0) {
            //::     $room_id = $dungeon->{'n_rooms'} + 1;
            //::     $dungeon->{'n_rooms'} = $room_id;
            //::   } else {
            //::     return $dungeon;
            //::   }
            //::   $dungeon->{'last_room_id'} = $room_id;
            */
            using (logger.BeginScope("{name}({rect})", nameof(emplace_room_collisiontest), rect))
            {
                //RASTER: (coords,if any, comes from a...) HEMI: EXCLUSIVE <0,0>..<nrows/2,ncols/2>
                //   # check for collisions with existing rooms
                if (TrySoundRoom(dungeon, rect, out var block, out var hit))
                {
                    logger.LogDebug("Room {r} @[{extents}]: approved because no hits", MyRoomIssuer.Current, rect.ToString());
                    var issued = MyRoomIssuer.TryIssueRoom(out proposed_room_id);
                    logger.LogDebug("{outcome} -- current id:{id}", issued ? "issued room" : "Ran out of rooms?", MyRoomIssuer.Current);
                    return issued;
                }
                else if (block)
                {
                    logger.LogTrace("sounding resulted in block");
                    proposed_room_id = null;
                    return false;
                }
                else
                {
                    logger.LogInformation("Room {r} @[{extents}]: rejected because hits={h} > 0 ", MyRoomIssuer.Current, rect.ToString(), hit.Count);
                    proposed_room_id = null;
                    return false;
                }
            }
        }

        [Obsolete("Prefer Rectangles", error: true)]
        public bool emplace_room_collisiontest(
                IDungeon dungeon,
                [MaybeNullWhen(false), NotNullWhen(true)] out int? proposed_room_id,
                Realspace<int> r1, Realspace<int> c1, Realspace<int> r2, Realspace<int> c2)
                => emplace_room_collisiontest(dungeon, out proposed_room_id,
                    rect: new(new(c1, r1, c2 - c1 + 1, r2 - r1 + 1)));

        /// <summary>
        /// Sets a perimeter around the ROOM blocks. Block corridors from room boundary; check for door openings from adjacent rooms
        /// </summary>
        public void emplace_room_BlockPerimeter(IDungeon dungeon, Realspace<Rectangle> rect)
        {
            emplace_room_BlockPerimeter(dungeon, rect.Value.Top, rect.Value.Left, rect.Value.Bottom - 1, rect.Value.Right - 1);
        }

        /// <summary>
        /// Sets a perimeter around the ROOM blocks. Block corridors from room boundary; check for door openings from adjacent rooms
        /// </summary>
        /// <param name="dungeon"></param>
        /// <param name="r1"></param>
        /// <param name="c1"></param>
        /// <param name="r2"></param>
        /// <param name="c2"></param>
        public void emplace_room_BlockPerimeter(IDungeon dungeon, Realspace<int> r1, Realspace<int> c1, Realspace<int> r2, Realspace<int> c2)
        {
            /*
            //::   for ($r = $r1 - 1; $r <= $r2 + 1; $r++) {
            //::     unless ($cell->[$r][$c1 - 1] & ($ROOM | $ENTRANCE)) {
            //::       $cell->[$r][$c1 - 1] |= $PERIMETER;
            //::     }
            //::     unless ($cell->[$r][$c2 + 1] & ($ROOM | $ENTRANCE)) {
            //::       $cell->[$r][$c2 + 1] |= $PERIMETER;
            //::     }
            //::   }
            //::   for ($c = $c1 - 1; $c <= $c2 + 1; $c++) {
            //::     unless ($cell->[$r1 - 1][$c] & ($ROOM | $ENTRANCE)) {
            //::       $cell->[$r1 - 1][$c] |= $PERIMETER;
            //::     }
            //::     unless ($cell->[$r2 + 1][$c] & ($ROOM | $ENTRANCE)) {
            //::       $cell->[$r2 + 1][$c] |= $PERIMETER;
            //::     }
            //::   }
            */
            void PerimeterizeIfNonroomNonentrance(int r, int c)
            {
                if (!dungeon.cell[r, c].HasAnyFlag(Cellbits.ROOM | Cellbits.ENTRANCE)) //! or-entrance is NOT an alteration vs perl!
                {
                    dungeon.cell[r, c] |= Cellbits.PERIMETER;
                }
            }
            using (logger.BeginScope("{name}({args})", nameof(emplace_room_BlockPerimeter), new { r1, c1, r2, c2 }))
            {
                logger.LogTrace("Placing Perimeter");
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

        /// <summary>
        /// propose room position and size, invoking 4 randoms.
        /// </summary>
        /// <param name="prototuple">a proposed point</param>
        /// <returns> a rectangle in hemispace </returns>
        private Hemispace<Rectangle> set_room(IDungeon dungeon, Hemispace<Coord>? prototuple)
        {
            /*
            //:: sub set_room {
            //::   my ($dungeon,$proto) = @_;
            //::   my $base = $dungeon->{'room_base'};
            //::   my $radix = $dungeon->{'room_radix'};
            //:: 
            //::   unless (defined $proto->{'height'}) {
            //::     if (defined $proto->{'i'}) {
            //::       my $a = $dungeon->{'n_i'} - $base - $proto->{'i'};
            //::          $a = 0 if ($a < 0);
            //::       my $r = ($a < $radix) ? $a : $radix;
            //:: 
            //::       $proto->{'height'} = int(rand($r)) + $base;
            //::     } else {
            //::       $proto->{'height'} = int(rand($radix)) + $base;
            //::     }
            //::   }
            //::   unless (defined $proto->{'width'}) {
            //::     if (defined $proto->{'j'}) {
            //::       my $a = $dungeon->{'n_j'} - $base - $proto->{'j'};
            //::          $a = 0 if ($a < 0);
            //::       my $r = ($a < $radix) ? $a : $radix;
            //:: 
            //::       $proto->{'width'} = int(rand($r)) + $base;
            //::     } else {
            //::       $proto->{'width'} = int(rand($radix)) + $base;
            //::     }
            //::   }
            //::   unless (defined $proto->{'i'}) {
            //::     $proto->{'i'} = int(rand($dungeon->{'n_i'} - $proto->{'height'}));
            //::   }
            //::   unless (defined $proto->{'j'}) {
            //::     $proto->{'j'} = int(rand($dungeon->{'n_j'} - $proto->{'width'}));
            //::   }
            //::   return $proto;
            //:: }
            */
            using (logger.BeginScope(nameof(set_room)))
            {
                var roomBase = settings.Value.Rooms.room_base;//? even, if room_minmax are obligate odd
                var roomRadix = settings.Value.Rooms.room_radix; //? odd, if room_minmax are obligate odd
                int CreateRadix(int hemicelladdress, int hemicellmax)
                    => Math.Min(roomRadix, Math.Max(0, hemicellmax - roomBase - hemicelladdress));
                //! For legacy compatability, Size must be random'd before finding Point ij
                //! AND this is per-parameter granularity: height/width and x/y order matter too. it MUST be [Height,Width,Row,Col]
                Rectangle ans = new() { Height = 0, Width = 0, Y = 0, X = 0 };
                var radix = prototuple is Hemispace<Coord> hptrdx
                    ? (h: CreateRadix(hptrdx.Value.r, dungeon.n_i), w: CreateRadix(hptrdx.Value.c, dungeon.n_j))
                    : (h: roomRadix, w: roomRadix);
                ans.Height = random.Next(radix.h) + roomBase; // random invocation 1: Height
                ans.Width = random.Next(radix.w) + roomBase; // random invocation 2: Width
                (int h, int w) pos = prototuple is Hemispace<Coord> hpt
                    ? ((int h, int w))hpt.Value
                    : (h: dungeon.n_i - ans.Height, w: dungeon.n_j - ans.Width);
                ans.Y = random.Next(pos.h); // random invocation 3: Row
                ans.X = random.Next(pos.w); // random invocation 4: Col
                return new Hemispace<Rectangle>(ans);
            }
        }

        #region Sounding Room
        /// <summary>test cells in the inclusive region. 
        /// - if any are blocked, set a return flag. 
        /// - if any belong to a designated room, produce a count of how many per room
        /// <remarks>
        /// {
        /// blocked=1
        /// }
        /// </remarks></summary>
        /// <param name="dungeon"></param>
        /// <param name="rect"></param>
        /// <returns>count of hits for each roomid, or a dictionary containing the key 'blocked'</returns>
        private Dictionary<string, int> sound_room(IDungeon dungeon, Realspace<Rectangle> rect)
        {
            /*
            //:: sub sound_room {
            //::   my ($dungeon,$r1,$c1,$r2,$c2) = @_;
            //::   my $cell = $dungeon->{'cell'};
            //::   my $hit;
            //:: 
            //::   my $r; for ($r = $r1; $r <= $r2; $r++) {
            //::     my $c; for ($c = $c1; $c <= $c2; $c++) {
            //::       if ($cell->[$r][$c] & $BLOCKED) {
            //::         return { 'blocked' => 1 };
            //::       }
            //::       if ($cell->[$r][$c] & $ROOM) {
            //::         my $id = ($cell->[$r][$c] & $ROOM_ID) >> 6;
            //::         $hit->{$id} += 1;
            //::       }
            //::     }
            //::   }
            //::   return $hit;
            //:: }
            */
            using (logger.BeginScope("{}{}", nameof(sound_room), rect.Value))
            {
                Dictionary<string, int> hit = [];

                static void IncrementCount(Dictionary<string, int> self, string key)
                {
                    self[key] = self.TryGetValue(key, out int prevhitcount) ? prevhitcount + 1 : 1;
                }

                //RASTER: REALSPACE: INCLUSIVE <r1,c1>..<r2,c2>
                foreach (var (r, c) in Dim2d.RangeUpperExclusive(rect).AsRealspace())
                {
                    if (dungeon.cell[r, c].HasFlag(Cellbits.BLOCKED))
                    {
                        return new Dictionary<string, int> { ["blocked"] = 1 };
                    }
                    if (dungeon.cell[r, c].TryGetRoomId(out int roomid))
                    {
                        IncrementCount(hit, roomid.ToString());
                    }
                }
                logger.LogDebug("sounding {rect}: {hits}", rect.ToJson(), string.Join(",", hit.Select(kvp => $"'{kvp.Key}'={kvp.Value}")));
                return hit;
            }
        }

        /// <summary>
        /// test the room for viability
        /// </summary> 
        /// <returns></returns>
        [Obsolete("Prefer Rectangles", error: true)]
        private Dictionary<string, int> sound_room(IDungeon dungeon, Realspace<int> r1, Realspace<int> c1, Realspace<int> r2, Realspace<int> c2)
            => sound_room(dungeon, new(new(c1, r1, c2 - c1, r2 - r1)));

        /// <summary>
        /// test the room and categorize the outcome
        /// </summary>
        /// <param name="rect">proposed room</param>
        /// <param name="blocked">Set if sounding encountered a <see cref="Cellbits.BLOCKED"/> cell</param>
        /// <param name="hit">record of the sounding</param>
        /// <returns>true if any hits occurred</returns>
        protected bool TrySoundRoom(IDungeon dungeon, Realspace<Rectangle> rect, out bool blocked, out Dictionary<string, int> hit)
        {
            hit = sound_room(dungeon, rect);
            blocked = hit.ContainsKey("blocked");
            return hit is { Count: 0 };
        }
        #endregion Sounding Room
        #endregion place rooms

    }
}
