namespace Donjon;

using System.Collections;

using Microsoft.Extensions.Options;

using TRoomId = int;

public interface IDungeonRoomIssuer
{
    /// <summary>number of room_ids issued (and the source counter for issuing them)  </summary>
    public TRoomId n_rooms { get; } //? should this be a member of opts?

    /// <summary>last room_id issued</summary>
    public TRoomId? last_room_id { get; }
    public bool TryIssueRoom(out TRoomId id);
}

public class RoomIdIssuer(IOptions<RoomSettings> roomSettings, IOptions<DungeonSettings> dSettings)
    : IEnumerator<TRoomId>, IDungeonRoomIssuer
{
    object IEnumerator.Current => Current;
    public TRoomId Current { get; private set; } = 0;
    public TRoomId Maximum => alloc_rooms();

    public TRoomId n_rooms => Current;
    public TRoomId? last_room_id => Current - 1 < 0 ? null : Current - 1;

    public void Dispose() { }

    public bool MoveNext() => Current < Maximum ? Current++ < Maximum : false;

    public void Reset() => Current = 0;

    /// <summary> allocate number of rooms based on the ratio of dungeon area:room area   (h*w)/(roommax^2) </summary>
    /// <remarks><code>
    /// sub alloc_rooms {
    ///   my ($dungeon) = @_;
    ///   my $dungeon_area = $dungeon->{'n_cols'} * $dungeon->{'n_rows'};
    ///   my $room_area = $dungeon->{'room_max'} * $dungeon->{'room_max'};
    ///   my $n_rooms = int($dungeon_area / $room_area);
    /// 
    ///   return $n_rooms;
    /// }
    /// </code></remarks>
    TRoomId alloc_rooms()
    {
        int dungeon_area = dSettings.Value.n_cols * dSettings.Value.n_rows;
        int room_area = roomSettings.Value.room_max * roomSettings.Value.room_max; // will not be zero if any room at all
        return dungeon_area / room_area;
    }

    public bool TryIssueRoom(out TRoomId id)
    {
        id = Current;
        return MoveNext();
    }
}
