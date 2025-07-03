
using System.Text.Json;

using Donjon.Original;
using Donjon.Rooms.Commands;

using Microsoft.Extensions.Logging;

namespace Donjon
{
    public partial class DungeonGenRefactored
    {

        #region doors/corridors

        ///<summary> emplace openings for doors and corridors</summary>
        /// <remarks><code>
        /// sub open_rooms {
        ///   my ($dungeon) = @_;
        /// 
        ///   my $id; for ($id = 1; $id <= $dungeon->{'n_rooms'}; $id++) {
        ///     $dungeon = &open_room($dungeon,$dungeon->{'room'}[$id]);
        ///   }
        ///   delete($dungeon->{'connect'});
        ///   return $dungeon;
        /// }
        /// </code></remarks>
        /// <param name="dungeon"></param>
        /// <returns></returns>
        Dungeon open_rooms(Dungeon dungeon)
        {
            using (logger.BeginScope(nameof(open_rooms)))
            {
                logger.LogDebug("Open rooms, batch of {n}", dungeon.room.Count);
                foreach (var r in dungeon.room)
                {
                    dungeon = open_room(dungeon, r.Value);

                }
                dungeon.connect?.Clear();
                return dungeon;
            }
        }

        ///<summary>emplace openings for doors and corridors</summary>
        /// <remarks><code>
        /// sub open_room {
        ///   my ($dungeon,$room) = @_;
        ///   my @list = &door_sills($dungeon,$room);
        ///      return $dungeon unless (@list);
        ///   my $n_opens = &alloc_opens($dungeon,$room);
        ///   my $cell = $dungeon->{'cell'};
        /// 
        ///   my $i; for ($i = 0; $i < $n_opens; $i++) {
        ///     my $sill = splice(@list,int(rand(@list)),1);
        ///        last unless ($sill);
        ///     my $door_r = $sill->{'door_r'};
        ///     my $door_c = $sill->{'door_c'};
        ///     my $door_cell = $cell->[$door_r][$door_c];
        ///        redo if ($door_cell & $DOORSPACE);
        /// 
        ///     my $out_id; if ($out_id = $sill->{'out_id'}) {
        ///       my $connect = join(',',(sort($room->{'id'},$out_id)));
        ///       redo if ($dungeon->{'connect'}{$connect}++);
        ///     }
        ///     my $open_r = $sill->{'sill_r'};
        ///     my $open_c = $sill->{'sill_c'};
        ///     my $open_dir = $sill->{'dir'};
        /// 
        ///     # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        ///     # open door
        /// 
        ///     my $x; for ($x = 0; $x < 3; $x++) {
        ///       my $r = $open_r + ($di->{$open_dir} * $x);
        ///       my $c = $open_c + ($dj->{$open_dir} * $x);
        /// 
        ///       $cell->[$r][$c] &= ~ $PERIMETER;
        ///       $cell->[$r][$c] |= $ENTRANCE;
        ///     }
        ///     my $door_type = &door_type();
        ///     my $door = { 'row' => $door_r, 'col' => $door_c };
        /// 
        ///     if ($door_type == $ARCH) {
        ///       $cell->[$door_r][$door_c] |= $ARCH;
        ///       $door->{'key'} = 'arch'; $door->{'type'} = 'Archway';
        ///     } elsif ($door_type == $DOOR) {
        ///       $cell->[$door_r][$door_c] |= $DOOR;
        ///       $cell->[$door_r][$door_c] |= (ord('o') << 24);
        ///       $door->{'key'} = 'open'; $door->{'type'} = 'Unlocked Door';
        ///     } elsif ($door_type == $LOCKED) {
        ///       $cell->[$door_r][$door_c] |= $LOCKED;
        ///       $cell->[$door_r][$door_c] |= (ord('x') << 24);
        ///       $door->{'key'} = 'lock'; $door->{'type'} = 'Locked Door';
        ///     } elsif ($door_type == $TRAPPED) {
        ///       $cell->[$door_r][$door_c] |= $TRAPPED;
        ///       $cell->[$door_r][$door_c] |= (ord('t') << 24);
        ///       $door->{'key'} = 'trap'; $door->{'type'} = 'Trapped Door';
        ///     } elsif ($door_type == $SECRET) {
        ///       $cell->[$door_r][$door_c] |= $SECRET;
        ///       $cell->[$door_r][$door_c] |= (ord('s') << 24);
        ///       $door->{'key'} = 'secret'; $door->{'type'} = 'Secret Door';
        ///     } elsif ($door_type == $PORTC) {
        ///       $cell->[$door_r][$door_c] |= $PORTC;
        ///       $cell->[$door_r][$door_c] |= (ord('#') << 24);
        ///       $door->{'key'} = 'portc'; $door->{'type'} = 'Portcullis';
        ///     }
        ///     $door->{'out_id'} = $out_id if ($out_id);
        ///     push(@{ $room->{'door'}{$open_dir} },$door) if ($door);
        ///   }
        ///   return $dungeon;
        /// }
        /// </code></remarks>
        /// <param name="dungeon"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        Dungeon open_room(Dungeon dungeon, IDungeonRoom room)
        {
            using (logger.BeginScope(nameof(open_room)))
            {
                logger.LogInformation("opening room {room}", JsonSerializer.Serialize(room, jsonLoggingOptions));
                IEnumerable<Sill> sills_list = door_sills(dungeon, room);
                if (sills_list is null) return dungeon;
                int n_opens = alloc_opens(dungeon, room);

                #region OpenManyDoorsCommand? (n_opens,sills_list,dungeon, room )
                for (int i = 0; i < n_opens; i++)
                {
                    //     my $sill = splice(@list,int(rand(@list)),1);
                    //        last unless ($sill);
                    if (!sills_list.Any()) break;
                    if (OpenDoorCommand.TryCreate(dungeon.random, sills_list, out var doorCmd))
                    {
                        // ... add to registry (manydoors cmd?)
                    }
                    #region OpenDoorCmd (sill,dungeon,room)
                    Sill sill = sills_list.ElementAt(dungeon.random.Next(sills_list.Count()));

                    int sill_door_r = sill.door_r;
                    int sill_door_c = sill.door_c;
                    //~~ var door_cell = dungeon.cell[sill_door_r, sill_door_c];
                    //        redo if ($door_cell & $DOORSPACE);
                    //! `redo` is a perl notion that C# cannot replicate: If the condition inside the if statement is true, 
                    //! the loop restarts from the beginning, AFTER the increment is applied
                    // ? is this why testing for connections by name is needed?
                    // ? when combined with sillfetching by `dungeon.random.Next(list.Count())`,
                    // ? it will walk past already succesful sills until it can try a new one
                    if (dungeon.cell[sill.door_r, sill.door_c].HasAnyFlag(Cellbits.DOORSPACE)) { i = -1; continue; }

                    switch (sill.AttemptConnection(dungeon, room))
                    {
                        case SillHelper.SillActionKind.EXCEEDS_PERIMETER:
                            continue;
                        case SillHelper.SillActionKind.RESTART:
                            i = -1;
                            continue;
                        case SillHelper.SillActionKind.CONNECTION_MADE: break;
                        case SillHelper.SillActionKind.CONTINUE: break;
                    }
                    ;
                    var open_r = sill.sill_r;
                    var open_c = sill.sill_c;
                    Cardinal open_dir = sill.dir;

                    /// # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
                    //     # open door
                    using (logger.BeginScope("OpenDoor"))
                    {
                        for (int x = 0; x < 3; x++)// len 4
                        {
                            int r = open_r + (di[open_dir] * x);
                            int c = open_c + (dj[open_dir] * x);

                            dungeon.cell[r, c] &= ~Cellbits.PERIMETER;
                            dungeon.cell[r, c] |= Cellbits.ENTRANCE;
                        }
                        Cellbits door_type = getdoor_type(dungeon.random);

                        try
                        {
                            (char? sign, string key, string type) doorinfo = Lookup_doorinfo(door_type);

                            logger.LogTrace("working on door {d} at ({r},{c})", doorinfo, sill_door_r, sill_door_c);
                            dungeon.cell[sill_door_r, sill_door_c] |= door_type;
                            dungeon.cell[sill_door_r, sill_door_c].SetLabel(doorinfo.sign ?? (char)0);
                            var door = new DoorData
                            {
                                row = sill_door_r,
                                col = sill_door_c,
                                open_dir = open_dir,
                                key = doorinfo.key,
                                type = doorinfo.type,
                                out_id = sill.out_id
                            };

                            //     push(@{ $room->{'door'}{$open_dir} },$door) if ($door); 
                            if (!room.door.TryAdd(open_dir, [door])) room.door[open_dir].Add(door);
                            logger.LogInformation("Add door {d} at ({r},{c})", doorinfo, sill_door_r, sill_door_c);
                        }
                        catch (InvalidOperationException _)
                        {
                            logger.LogTrace("rejected door at ({r},{c})", sill_door_r, sill_door_c);
                        }

                    }// /scope Opendoor
                    #endregion OpenDoorCmd  
                }
                #endregion OpenManyDoorsCommand
                logger.LogInformation("Opened {actual} doors for {expected} attempts", room.door.Sum(directionkvp => directionkvp.Value.Count), n_opens);
                return dungeon;
            }
        }

        ///<summary> allocate number of opens for the room, randomized upto sqrt(hemispace area)</summary>
        /// <remarks><code>
        /// sub alloc_opens {
        ///   my ($dungeon,$room) = @_;
        ///   my $room_h = (($room->{'south'} - $room->{'north'}) / 2) + 1;
        ///   my $room_w = (($room->{'east'} - $room->{'west'}) / 2) + 1;
        ///   my $flumph = int(sqrt($room_w * $room_h));
        ///   my $n_opens = $flumph + int(rand($flumph));
        /// 
        ///   return $n_opens;
        /// }
        /// </code></remarks>
        /// <returns>number of openings for the room</returns>
        int alloc_opens(Dungeon dungeon, IDungeonRoom room)
        {
            using (logger.BeginScope(nameof(alloc_opens)))
            {
                int room_h = (room.width / 2) + 1;
                int room_w = (room.height / 2) + 1;
                int flumph = (int)Math.Sqrt(room_w * room_h);
                return flumph + dungeon.random.Next(flumph);
            }
        }


        ///<summary> list available sills, shuffled </summary>
        /// <remarks><code>
        /// sub door_sills {
        ///   my ($dungeon,$room) = @_;
        ///   my $cell = $dungeon->{'cell'};
        ///   my @list;
        /// 
        ///   if ($room->{'north'} >= 3) {
        ///     my $c; for ($c = $room->{'west'}; $c <= $room->{'east'}; $c += 2) {
        ///       my $sill = &check_sill($cell,$room,$room->{'north'},$c,'north');
        ///       push(@list,$sill) if ($sill);
        ///     }
        ///   }
        ///   if ($room->{'south'} <= ($dungeon->{'n_rows'} - 3)) {
        ///     my $c; for ($c = $room->{'west'}; $c <= $room->{'east'}; $c += 2) {
        ///       my $sill = &check_sill($cell,$room,$room->{'south'},$c,'south');
        ///       push(@list,$sill) if ($sill);
        ///     }
        ///   }
        ///   if ($room->{'west'} >= 3) {
        ///     my $r; for ($r = $room->{'north'}; $r <= $room->{'south'}; $r += 2) {
        ///       my $sill = &check_sill($cell,$room,$r,$room->{'west'},'west');
        ///       push(@list,$sill) if ($sill);
        ///     }
        ///   }
        ///   if ($room->{'east'} <= ($dungeon->{'n_cols'} - 3)) {
        ///     my $r; for ($r = $room->{'north'}; $r <= $room->{'south'}; $r += 2) {
        ///       my $sill = &check_sill($cell,$room,$r,$room->{'east'},'east');
        ///       push(@list,$sill) if ($sill);
        ///     }
        ///   }
        ///   return &shuffle(@list);
        /// }
        /// </code></remarks>
        /// <param name="dungeon"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        IEnumerable<Sill> door_sills(Dungeon dungeon, IDungeonRoom room)
        {
            using (logger.BeginScope(nameof(door_sills)))
            {
                List<Sill> list = [];
                if (room.north >= 3) // if north is inset 3 from map's top edge, there's enough room to exit north
                {
                    for (int c = room.west; c <= room.east; c += 2) // Note step by 2
                    {
                        Sill? sill = check_sill(dungeon.cell, room, sill_r: room.north, sill_c: c, Cardinal.north);
                        if (sill.HasValue) list.Add(sill.Value);
                    }
                }
                if (room.south <= dungeon.n_rows - 3)// if south is inset 3 from map's bottom edge, there's enough room to exit south
                {
                    for (int c = room.west; c <= room.east; c += 2)
                    {
                        Sill? sill = check_sill(dungeon.cell, room, sill_r: room.south, sill_c: c, Cardinal.south);
                        if (sill.HasValue) list.Add(sill.Value);
                    }
                }
                if (room.west >= 3) // if west is inset 3 from map's left, there's enough room to exit west
                {
                    for (int r = room.north; r <= room.south; r += 2)
                    {
                        Sill? sill = check_sill(dungeon.cell, room, r, room.west, Cardinal.west);
                        if (sill.HasValue) list.Add(sill.Value);
                    }
                }
                if (room.east <= dungeon.n_cols - 3) // if east is inset 3 from map's right, there's enough room to exit east
                {
                    for (int r = room.north; r <= room.south; r += 2)
                    {
                        Sill? sill = check_sill(dungeon.cell, room, r, room.east, Cardinal.east);
                        if (sill.HasValue) list.Add(sill.Value);
                    }
                }
                var ans = list.ToArray();
                dungeon.random.Shuffle(ans);
                logger.LogInformation("Produced {n} sills for room {rid}", ans.Length, room.id);
                return ans;
            }
        }

        /// <summary>
        /// test the indicated sill is valid, return a new <see cref="Sill"/> if it is
        /// </summary>
        /// <remarks><code>
        /// sub check_sill {
        ///   my ($cell,$room,$sill_r,$sill_c,$dir) = @_;
        ///   my $door_r = $sill_r + $di->{$dir};
        ///   my $door_c = $sill_c + $dj->{$dir};
        ///   my $door_cell = $cell->[$door_r][$door_c];
        ///      return unless ($door_cell & $PERIMETER);
        ///      return if ($door_cell & $BLOCK_DOOR);
        ///   my $out_r  = $door_r + $di->{$dir};
        ///   my $out_c  = $door_c + $dj->{$dir};
        ///   my $out_cell = $cell->[$out_r][$out_c];
        ///      return if ($out_cell & $BLOCKED);
        ///
        ///   my $out_id; if ($out_cell & $ROOM) {
        ///     $out_id = ($out_cell & $ROOM_ID) >> 6;
        ///     return if ($out_id == $room->{'id'});
        ///   }
        ///   return {
        ///     'sill_r'    => $sill_r,
        ///     'sill_c'    => $sill_c,
        ///     'dir'       => $dir,
        ///     'door_r'    => $door_r,
        ///     'door_c'    => $door_c,
        ///     'out_id'    => $out_id,
        ///   };
        /// }
        /// </code></remarks>
        /// <param name="cell"></param>
        /// <param name="room"></param>
        /// <param name="sill_r"></param>
        /// <param name="sill_c"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        Sill? check_sill(Cellbits[,] cell, IDungeonRoom room, Realspace<int> sill_r, Realspace<int> sill_c, Cardinal dir)
        {
            using (logger.BeginScope(nameof(check_sill)))
            {
                (int door_r, int door_c) = (sill_r + di[dir], sill_c + dj[dir]);
                //      return unless ($door_cell & $PERIMETER);
                if (false == cell[door_r, door_c].HasFlag(Cellbits.PERIMETER))
                {
                    return null; // return because of not being a perimeter
                }
                //      return if ($door_cell & $BLOCK_DOOR);
                if (cell[door_r, door_c].HasFlag(Cellbits.BLOCK_DOOR))
                {
                    return null; // because of being blocked
                }

                (int out_r, int out_c) = (door_r + di[dir], door_c + dj[dir]);
                Cellbits out_cell = cell[out_r, out_c]; // the first space outside the door cell
                if (out_cell.HasFlag(Cellbits.BLOCKED)) { return null; } // cannot open into a block

                if (out_cell.TryGetRoomId(out int out_id)) // cannot open into the same room served by the door
                {
                    if (out_id == room.id) return null;
                }
                return new()
                {
                    sill_r = sill_r,
                    sill_c = sill_c,
                    dir = dir,
                    door_r = door_r,
                    door_c = door_c,
                    out_id = out_id,
                };
            }
        }

        /// <summary> random door type </summary>
        /// <remarks><code>
        /// sub door_type {
        ///   my $i = int(rand(110));
        /// 
        ///   if ($i < 15) {
        ///     return $ARCH;
        ///   } elsif ($i < 60) {
        ///     return $DOOR;
        ///   } elsif ($i < 75) {
        ///     return $LOCKED;
        ///   } elsif ($i < 90) {
        ///     return $TRAPPED;
        ///   } elsif ($i < 100) {
        ///     return $SECRET;
        ///   } else {
        ///     return $PORTC;
        ///   }
        /// }
        /// <code></remarks>
        Cellbits getdoor_type(Random r)
        {
            using (logger.BeginScope(nameof(getdoor_type)))
            {
                return r.Next(110) switch
                {
                    >= 00 and < 15 => Cellbits.DOOR_ARCH,
                    >= 15 and < 60 => Cellbits.DOOR_SIMPLE,
                    >= 60 and < 75 => Cellbits.DOOR_LOCKED,
                    >= 75 and < 90 => Cellbits.DOOR_TRAPPED,
                    >= 90 and < 100 => Cellbits.DOOR_SECRET,
                    _ => Cellbits.DOOR_PORTC
                };
            }
        }
        #endregion doors/corridors


        /// <summary>
        /// 
        /// </summary>
        /// <param name="door_type"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">if non-doorspace or if undefined doorinfo</exception>
        public static (char? sign, string key, string type) Lookup_doorinfo(Cellbits door_type)
        {
            if (!door_type.HasAnyFlag(Cellbits.DOORSPACE))
                throw new InvalidOperationException($"SWW: Not in Doorspace: {door_type}");

            return door_type switch
            {
                Cellbits.DOOR_ARCH => (sign: null, key: "arch", type: "Archway"),
                Cellbits.DOOR_SIMPLE => (sign: 'o', key: "open", type: "Unlocked Door"),
                Cellbits.DOOR_LOCKED => (sign: 'x', key: "lock", type: "Locked Door"),
                Cellbits.DOOR_TRAPPED => (sign: 't', key: "trap", type: "Trapped Door"),
                Cellbits.DOOR_SECRET => (sign: 's', key: "secret", type: "Secret Door"),
                Cellbits.DOOR_PORTC => (sign: '#', key: "portc", type: "Portcullis"),
                _ => throw new InvalidOperationException($"SWW: No sign/key/type on record: {door_type}"),
            };
        }
    }

    static class SillHelper
    {
        public enum SillActionKind
        {
            CONTINUE,
            /// <summary>
            /// Successfully added the sill to dungeon's connection dictionary
            /// </summary>
            CONNECTION_MADE,

            /// <summary>
            ///  no further connections possible, SKIP FURTHER DOOR OPS
            /// </summary>
            EXCEEDS_PERIMETER,
            /// <summary>
            /// increment dungeon's connection count, and RESTART
            /// </summary>
            RESTART,
        }
        /// <summary>
        /// effects: 
        /// </summary>
        /// <param name="sill"></param>
        /// <param name="dungeon"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        public static SillActionKind AttemptConnection(
            this Sill sill, Dungeon dungeon, IDungeonRoom room
            )
        {
            if (sill.out_id.HasValue)
            {
                //? constructs the connection string and checks if it has been used before
                //       my $connect = join(',',(sort($room->{'id'},$out_id)));
                string connect = string.Join(",", Enumerable.Order([room.id, sill.out_id.Value]));
                //       redo if ($dungeon->{'connect'}{$connect}++);
                if (dungeon.connect?.TryGetValue(connect, out int connectionCount) ?? false)
                {
                    if (connectionCount > room.Perimeter)
                    {
                        // logger.LogDebug("count ({v}) exceeds room perimiter ({p}) for connection {conn}",
                        //     connectionCount, room.Perimeter, connect);
                        // continue;
                        // CASE 1: no further connections possible, SKIP FURTHER DOOR OPS
                        return SillActionKind.EXCEEDS_PERIMETER;
                    }
                    dungeon.connect![connect] = connectionCount + 1;
                    // CASE 2: increment connection count, and RESTART
                    //! `redo` is a perl notion that C# cannot replicate: If the condition inside the if statement is true, 
                    //! the loop restarts from the beginning, AFTER the increment is applied
                    // i = -1; continue;
                    return SillActionKind.RESTART;
                }
                else if (dungeon.connect?.TryAdd(connect, 1) ?? false)
                {
                    // Successfully added the sill to dungeon's connection dictionary
                    //! adds, but does not restart
                    return SillActionKind.CONNECTION_MADE;
                }
            }
            return SillActionKind.CONTINUE;
        }
    }
    namespace Rooms.Commands
    {
        class OpenDoorCommand : MultipleTransactionBase<IDungeon>
        {
            required public int SillIndex { get; init; }
            required public Cellbits DoorType { get; init; }
            // ... other state

            /// <summary>
            /// 
            /// </summary>
            /// <see cref="DungeonGenRefactored.open_room(Dungeon, IDungeonRoom)"/>
            /// <param name="rng"></param>
            /// <param name="sills"></param>
            /// <param name="dun"></param>
            public OpenDoorCommand(Random rng,
             List<Sill> sills, IDungeon dun)
            {
                SillIndex = rng.Next(sills.Count);
                DoorType = GetDoorType(rng);
                // ... store other random choices

                Sill sill = sills.ElementAt(SillIndex);

                foreach (var c in Enumerable.Range(0, 4).Select(x => (
                    sill.sill_r + (DungeonGenRefactored.di[sill.dir] * x)
                    , sill.sill_c + (DungeonGenRefactored.dj[sill.dir] * x)
                    )))
                {
                    var storeme = new ReversibleSingleCellChangeCommand(
                      c.Item1, c.Item2,
                      dun.cell.At(c),
                      Cellbits.ENTRANCE | dun.cell.At(c) & ~Cellbits.PERIMETER);
                    this.Add(storeme);
                }


                (char? sign, string key, string type) = DungeonGenRefactored.Lookup_doorinfo(DoorType);
                var storemeToo = new ReversibleSingleCellChangeCommand(
                    sill.door_r, sill.door_c,
                    dun.cell[sill.door_r, sill.door_c],
                        (this.DoorType | dun.cell[sill.door_r, sill.door_c]).SetLabel(sign ?? (char)0));
                this.Add(storemeToo);
                var door = new DoorData
                {
                    row = sill.door_r,
                    col = sill.door_c,
                    open_dir = sill.dir,
                    key = key,
                    type = type,
                    out_id = sill.out_id
                };
                this.Add(default!);
            }

            /// <summary> random door type </summary>
            /// <remarks><code>
            /// sub door_type {
            ///   my $i = int(rand(110));
            /// 
            ///   if ($i < 15) {
            ///     return $ARCH;
            ///   } elsif ($i < 60) {
            ///     return $DOOR;
            ///   } elsif ($i < 75) {
            ///     return $LOCKED;
            ///   } elsif ($i < 90) {
            ///     return $TRAPPED;
            ///   } elsif ($i < 100) {
            ///     return $SECRET;
            ///   } else {
            ///     return $PORTC;
            ///   }
            /// }
            /// <code></remarks>
            private static Cellbits GetDoorType(Random rng)
            {
                return rng.Next(110) switch
                {
                    >= 00 and < 15 => Cellbits.DOOR_ARCH,
                    >= 15 and < 60 => Cellbits.DOOR_SIMPLE,
                    >= 60 and < 75 => Cellbits.DOOR_LOCKED,
                    >= 75 and < 90 => Cellbits.DOOR_TRAPPED,
                    >= 90 and < 100 => Cellbits.DOOR_SECRET,
                    _ => Cellbits.DOOR_PORTC
                };

            }

            public static bool TryCreate(Random rng, IEnumerable<Sill> sills, out OpenDoorCommand? cmd)
            {
                cmd = null;
                return false;
            }

            // Execute/Undo use SillIndex and DoorType, not new randoms
            // Effects:
            // - 4x in sill-open directions: strips Perimeter, or's Entrance
            // - or's the doortype flag into the cell bits and sets the label bits to the doortype's sign
            // - ADDS a DoorData to the room's reflist for the appropriate edge
            //    UNLESS
        }
    }
}