using System.Diagnostics.CodeAnalysis;

using Donjon.Original;
using Donjon.Pipeline;

using Microsoft.Extensions.Logging;

namespace Donjon;
#pragma warning disable IDE1006 // Naming Styles

public partial class DungeonGenRefactored
{
    public class LabelRoomsStep(ILogger<LabelRoomsStep> logger) : IPipelineOperation<IDungeon>
    {
        public bool TryInvoke(IDungeon input, [MaybeNullWhen(false), NotNullWhen(true)] out IDungeon? result)
        {
            result = label_rooms(input);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dungeon"></param>
        /// <returns></returns>
        IDungeon label_rooms(IDungeon dungeon)
        {
            /*
            //:: sub label_rooms {
            //::   my ($dungeon) = @_;
            //::   my $cell = $dungeon->{'cell'};
            //:: 
            //::   my $id; for ($id = 1; $id <= $dungeon->{'n_rooms'}; $id++) {
            //::     my $room = $dungeon->{'room'}[$id];
            //::     my $label = "$room->{'id'}";
            //::     my $len = length($label);
            //::     my $label_r = int(($room->{'north'} + $room->{'south'}) / 2);
            //::     my $label_c = int(($room->{'west'} + $room->{'east'} - $len) / 2) + 1;
            //:: 
            //::     my $c; for ($c = 0; $c < $len; $c++) {
            //::       my $char = substr($label,$c,1);
            //::       $cell->[$label_r][$label_c + $c] |= (ord($char) << 24);
            //::     }
            //::   }
            //::   return $dungeon;
            //:: }
            */
            using (logger.BeginScope(nameof(label_rooms)))
            {
                foreach (IDungeonRoom room in dungeon.room.Values)
                {
                    string label = room.id.ToString();

                    // start writing the label from the room's middle row, and a column centered on the middle of the room
                    int label_r = (room.north + room.south) / 2;
                    int label_c = ((room.west + room.east - label.Length) / 2) + 1;
                    logger.LogInformation("Stamping id as label on cells of room id {id}, from {r},{c}", room.id, label_r, label_c);

                    for (int c = 0; c < label.Length; c++)
                    {
                        dungeon.cell.Set((label_r, label_c + c), v => v.SetLabel(label[c]));
                        dungeon.cell[label_r, label_c + c] = dungeon.cell[label_r, label_c + c].SetLabel(label[c]);
                    }
                }
                return dungeon;
            }
        }
    }
}
#pragma warning restore IDE1006 // Naming Styles
