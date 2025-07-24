namespace Donjon;

public interface IDungeonGenerator //: IDungeonDescriber
{
    public IDungeon Create_dungeon();
    protected IDungeonRoomIssuer RoomIssuer { get; }
}

public interface IDungeonDescriber<TOutput>
{
    public TOutput DescribeDungeonLite(IDungeon dungeon);
}

interface IRoomPlacement
{
    public void emplace_rooms();
}
#pragma warning restore IDE1006 // Naming Styles
