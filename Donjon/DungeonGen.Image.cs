// Adapted from https://donjon.bin.sh/code/dungeon/dungeon.pl
// https://creativecommons.org/licenses/by-nc/3.0/
using System.Drawing;

namespace Donjon;
public partial class DungeonGen
{
    #region Image Dungeon

    ///<summary></summary>
    ///<code>
    ///   my ($dungeon) = @_;
    ///   my $image = &scale_dungeon($dungeon);
    ///
    ///   # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    ///   # new image
    ///
    ///   my $ih = new GD::Image($image->{'width'},$image->{'height'},1);
    ///   my $pal = &get_palette($image,$ih);
    ///      $image->{'palette'} = $pal;
    ///   my $base = &base_layer($dungeon,$image,$ih);
    ///      $image->{'base_layer'} = $base;
    ///
    ///   $ih = &fill_image($dungeon,$image,$ih);
    ///   $ih = &open_cells($dungeon,$image,$ih);
    ///   $ih = &image_walls($dungeon,$image,$ih);
    ///   $ih = &image_doors($dungeon,$image,$ih);
    ///   $ih = &image_labels($dungeon,$image,$ih);
    ///
    ///   if ($dungeon->{'stair'}) {
    ///     $ih = &image_stairs($dungeon,$image,$ih);
    ///   }
    ///
    ///   # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    ///   # write image
    ///
    ///   open(OUTPUT,">$dungeon->{'seed'}.gif") and do {
    ///     print OUTPUT $ih->gif();
    ///     close(OUTPUT);
    ///   };
    ///   return "$dungeon->{'seed'}.gif";
    ///</code>
    [Obsolete("notimplemented")]
    string image_dungeon(Dungeon dungeon)
    {
        throw new NotImplementedException();
        //!var image = scale_dungeon(dungeon); //   my $image = &scale_dungeon($dungeon);
        //   # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        //   # new image
        //
        //   my $ih = new GD::Image($image->{'width'},$image->{'height'},1);
        //!Image ih = new Bitmap(image.width, image.height);
        //   my $pal = &get_palette($image,$ih);
        //      $image->{'palette'} = $pal;
        //   my $base = &base_layer($dungeon,$image,$ih);
        //      $image->{'base_layer'} = $base;
        //
        //   $ih = &fill_image($dungeon,$image,$ih);
        //   $ih = &open_cells($dungeon,$image,$ih);
        //   $ih = &image_walls($dungeon,$image,$ih);
        //   $ih = &image_doors($dungeon,$image,$ih);
        //   $ih = &image_labels($dungeon,$image,$ih);
        //
        //   if ($dungeon->{'stair'}) {
        //     $ih = &image_stairs($dungeon,$image,$ih);
        //   }
        //
        //   # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        //   # write image
        //
        //   open(OUTPUT,">$dungeon->{'seed'}.gif") and do {
        //     print OUTPUT $ih->gif();
        //     close(OUTPUT);
        //   };
        //!string filename = $"{dungeon.seed}.gif";
        //!ih.Save(filename);
        //!return filename;//   return "$dungeon->{'seed'}.gif";
    }

    #endregion
    //   # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # 
    #region Not-Yet-Converted-Perl
    /*
  # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # #
  # scale dungeon

  sub scale_dungeon {
  my ($dungeon) = @_;

  my $image = {
    'cell_size' => $dungeon->{'cell_size'},
    'map_style' => $dungeon->{'map_style'},
  };
  $image->{'width'}  = (($dungeon->{'n_cols'} + 1)
                     *   $image->{'cell_size'}) + 1;
  $image->{'height'} = (($dungeon->{'n_rows'} + 1)
                     *   $image->{'cell_size'}) + 1;
  $image->{'max_x'}  = $image->{'width'} - 1;
  $image->{'max_y'}  = $image->{'height'} - 1;

  if ($image->{'cell_size'} > 16) {
    $image->{'font'} = gdLargeFont;
  } elsif ($image->{'cell_size'} > 12) {
    $image->{'font'} = gdSmallFont;
  } else {
    $image->{'font'} = gdTinyFont;
  }
  $image->{'char_w'} = $image->{'font'}->width;
  $image->{'char_h'} = $image->{'font'}->height;
  $image->{'char_x'} = int(($image->{'cell_size'}
                     -      $image->{'char_w'}) / 2) + 1;
  $image->{'char_y'} = int(($image->{'cell_size'}
                     -      $image->{'char_h'}) / 2) + 1;

  return $image;
  }

  # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # #
  # get palette

  sub get_palette {
  my ($image,$ih) = @_;

  my $pal; if ($map_style->{$image->{'map_style'}}) {
    $pal = $map_style->{$image->{'map_style'}};
  } else {
    $pal = $map_style->{'Standard'};
  }
  my $key; foreach $key (keys %{ $pal }) {
    if (ref($pal->{$key}) eq 'ARRAY') {
      $pal->{$key} = $ih->colorAllocate(@{ $pal->{$key} });
    } elsif (-f $pal->{$key}) {
      my $tile; if ($tile = new GD::Image($pal->{$key})) {
        $pal->{$key} = $tile;
      } else {
        delete $pal->{$key};
      }
    } elsif ($pal->{$key} =~ /([0-9a-f]{2})([0-9a-f]{2})([0-9a-f]{2})/i) {
      $pal->{$key} = $ih->colorAllocate(hex($1),hex($2),hex($3));
    }
  }
  unless (defined $pal->{'black'}) {
    $pal->{'black'} = $ih->colorAllocate(0,0,0);
  }
  unless (defined $pal->{'white'}) {
    $pal->{'white'} = $ih->colorAllocate(255,255,255);
  }
  return $pal;
  }

  # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
  # get color

  sub get_color {
  my ($pal,$key) = @_;

  while ($key) {
    return $pal->{$key} if (defined $pal->{$key});
    $key = $color_chain->{$key};
  }
  return undef;
  }

  # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
  # select tile

  sub select_tile {
  my ($tile,$dim) = @_;
  my $src_x = int(rand(int($tile->width / $dim))) * $dim;
  my $src_y = int(rand(int($tile->height / $dim))) * $dim;

  return ($src_x,$src_y,$dim,$dim);
  }

  # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # #
  # base layer

  sub base_layer {
  my ($dungeon,$image,$ih) = @_;
  my $max_x = $image->{'max_x'};
  my $max_y = $image->{'max_y'};
  my $dim = $image->{'cell_size'};
  my $pal = $image->{'palette'};
  my ($color,$tile);

  if (defined ($tile = $pal->{'open_pattern'})) {
    $ih->setTile($tile);
    $ih->filledRectangle(0,0,$max_x,$max_y,gdTiled);
  } elsif (defined ($tile = $pal->{'open_tile'})) {
    my $r; for ($r = 0; $r <= $dungeon->{'n_rows'}; $r++) {
      my $c; for ($c = 0; $c <= $dungeon->{'n_cols'}; $c++) {
        $ih->copy($tile,($c * $dim),($r * $dim),&select_tile($tile,$dim));
      }
    }
  } elsif (defined ($color = $pal->{'open'})) {
    $ih->filledRectangle(0,0,$max_x,$max_y,$color);
  } elsif (defined ($tile = $pal->{'background'})) {
    $ih->setTile($tile);
    $ih->filledRectangle(0,0,$max_x,$max_y,gdTiled);
  } else {
    $ih->filledRectangle(0,0,$max_x,$max_y,$pal->{'white'});
    $ih->fill(0,0,$pal->{'white'});
  }
  if ($color = $pal->{'open_grid'}) {
    $ih = &image_grid($dungeon,$image,$color,$ih);
  } elsif ($color = $pal->{'grid'}) {
    $ih = &image_grid($dungeon,$image,$color,$ih);
  }
  my $base = $ih->clone();

  if (defined ($tile = $pal->{'background'})) {
    $ih->setTile($tile);
    $ih->filledRectangle(0,0,$max_x,$max_y,gdTiled);
  } else {
    $ih->filledRectangle(0,0,$max_x,$max_y,$pal->{'white'});
  }
  return $base;
  }

  # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # #
  # image grid

  sub image_grid {
  my ($dungeon,$image,$color,$ih) = @_;

  if ($dungeon->{'grid'} eq 'None') {
    # no grid
  } elsif ($dungeon->{'grid'} eq 'Hex') {
    $ih = &hex_grid($dungeon,$image,$color,$ih);
  } else {
    $ih = &square_grid($dungeon,$image,$color,$ih);
  }
  return $ih;
  }

  # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
  # square grid

  sub square_grid {
  my ($dungeon,$image,$color,$ih) = @_;
  my $dim = $image->{'cell_size'};

  my $x; for ($x = 0; $x <= $image->{'max_x'}; $x += $dim) {
    $ih->line($x,0,$x,$image->{'max_y'},$color);
  }
  my $y; for ($y = 0; $y <= $image->{'max_y'}; $y += $dim) {
    $ih->line(0,$y,$image->{'max_x'},$y,$color);
  }
  return $ih;
  }

  # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
  # hex grid

  sub hex_grid {
  my ($dungeon,$image,$color,$ih) = @_;
  my $dim = $image->{'cell_size'};
  my $dy = ($dim / 2.0);
  my $dx = ($dim / 3.4641016151);
  my $n_col = ($image->{'width'}  / (3 * $dx));
  my $n_row = ($image->{'height'} /      $dy );
     $ih->setAntiAliased($color);

  my $i; for ($i = 0; $i < $n_col; $i++) {
    my $x1 = $i * (3 * $dx);
    my $x2 = $x1 + $dx;
    my $x3 = $x1 + (3 * $dx);

    my $j; for ($j = 0; $j < $n_row; $j++) {
      my $y1 = $j * $dy;
      my $y2 = $y1 + $dy;

      if (($i + $j) % 2) {
        $ih->line($x1,$y1,$x2,$y2,gdAntiAliased);
        $ih->line($x2,$y2,$x3,$y2,gdAntiAliased);
      } else {
        $ih->line($x2,$y1,$x1,$y2,gdAntiAliased);
      }
    }
  }
  return $ih;
  }

  # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # #
  # fill dungeon image

  sub fill_image {
  my ($dungeon,$image,$ih) = @_;
  my $max_x = $image->{'max_x'};
  my $max_y = $image->{'max_y'};
  my $dim = $image->{'cell_size'};
  my $pal = $image->{'palette'};
  my ($color,$tile);

  if (defined ($tile = $pal->{'fill_pattern'})) {
    $ih->setTile($tile);
    $ih->filledRectangle(0,0,$max_x,$max_y,gdTiled);
  } elsif (defined ($tile = $pal->{'fill_tile'})) {
    my $r; for ($r = 0; $r <= $dungeon->{'n_rows'}; $r++) {
      my $c; for ($c = 0; $c <= $dungeon->{'n_cols'}; $c++) {
        $ih->copy($tile,($c * $dim),($r * $dim),&select_tile($tile,$dim));
      }
    }
  } elsif (defined ($color = $pal->{'fill'})) {
    $ih->filledRectangle(0,0,$max_x,$max_y,$color);
  } elsif (defined ($tile = $pal->{'background'})) {
    $ih->setTile($tile);
    $ih->filledRectangle(0,0,$max_x,$max_y,gdTiled);
  } else {
    $ih->filledRectangle(0,0,$max_x,$max_y,$pal->{'black'});
    $ih->fill(0,0,$pal->{'black'});
  }
  if (defined ($color = $pal->{'fill'})) {
    $ih->rectangle(0,0,$max_x,$max_y,$color);
  }
  if ($color = $pal->{'fill_grid'}) {
    $ih = &image_grid($dungeon,$image,$color,$ih);
  } elsif ($color = $pal->{'grid'}) {
    $ih = &image_grid($dungeon,$image,$color,$ih);
  }
  return $ih;
  }

  # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # #
  # open cells

  sub open_cells {
  my ($dungeon,$image,$ih) = @_;
  my $cell = $dungeon->{'cell'};
  my $dim = $image->{'cell_size'};
  my $base = $image->{'base_layer'};

  my $r; for ($r = 0; $r <= $dungeon->{'n_rows'}; $r++) {
    my $y1 = $r * $dim;
    my $y2 = $y1 + $dim;

    my $c; for ($c = 0; $c <= $dungeon->{'n_cols'}; $c++) {
      next unless ($cell->[$r][$c] & $OPENSPACE);

      my $x1 = $c * $dim;
      my $x2 = $x1 + $dim;

      $ih->copy($base,$x1,$y1,$x1,$y1,($dim+1),($dim+1));
    }
  }
  return $ih;
  }

  # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # #
  # image walls

  sub image_walls {
  my ($dungeon,$image,$ih) = @_;
  my $cell = $dungeon->{'cell'};
  my $dim = $image->{'cell_size'};
  my $pal = $image->{'palette'};
  my $color;

  my $r; for ($r = 0; $r <= $dungeon->{'n_rows'}; $r++) {
    my $y1 = $r * $dim;
    my $y2 = $y1 + $dim;

    my $c; for ($c = 0; $c <= $dungeon->{'n_cols'}; $c++) {
         next unless ($cell->[$r][$c] & $OPENSPACE);
      my $x1 = $c * $dim;
      my $x2 = $x1 + $dim;
      my $c1 = $cell->[$r][$c];

      if (defined ($color = $pal->{'wall'})) {
        unless ($cell->[$r-1][$c-1] & $OPENSPACE) {
          $ih->setPixel($x1,$y1,$color);
        }
        unless ($cell->[$r-1][$c] & $OPENSPACE) {
          $ih->line($x1,$y1,$x2,$y1,$color);
        }
        unless ($cell->[$r][$c-1] & $OPENSPACE) {
          $ih->line($x1,$y1,$x1,$y2,$color);
        }
        unless ($cell->[$r][$c+1] & $OPENSPACE) {
          $ih->line($x2,$y1,$x2,$y2,$color);
        }
        unless ($cell->[$r+1][$c] & $OPENSPACE) {
          $ih->line($x1,$y2,$x2,$y2,$color);
        }
      }
    }
  }
  return $ih;
  }

  # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # #
  # image doors

  sub image_doors {
  my ($dungeon,$image,$ih) = @_;
  my $list = $dungeon->{'door'};
     return $ih unless ($list);
  my $cell = $dungeon->{'cell'};
  my $dim = $image->{'cell_size'};
  my $a_px = int($dim / 6);
  my $d_tx = int($dim / 4);
  my $t_tx = int($dim / 3);
  my $pal = $image->{'palette'};
  my $arch_color = &get_color($pal,'wall');
  my $door_color = &get_color($pal,'door');

  my $door; foreach $door (@{ $list }) {
    my $r = $door->{'row'};
    my $y1 = $r * $dim;
    my $y2 = $y1 + $dim;
    my $c = $door->{'col'};
    my $x1 = $c * $dim;
    my $x2 = $x1 + $dim;

    my ($xc,$yc); if ($cell->[$r][$c-1] & $OPENSPACE) {
      $xc = int(($x1 + $x2) / 2);
    } else {
      $yc = int(($y1 + $y2) / 2);
    }
    my $attr = &door_attr($door);

    if ($attr->{'wall'}) {
      if ($xc) {
        $ih->line($xc,$y1,$xc,$y2,$arch_color);
      } else {
        $ih->line($x1,$yc,$x2,$yc,$arch_color);
      }
    }
    if ($attr->{'secret'}) {
      if ($xc) {
        my $yc = int(($y1 + $y2) / 2);

          $ih->line($xc-1,$yc-$d_tx,$xc+2,$yc-$d_tx,$door_color);
        $ih->line($xc-2,$yc-$d_tx+1,$xc-2,$yc-1,$door_color);
          $ih->line($xc-1,$yc,$xc+1,$yc,$door_color);
            $ih->line($xc+2,$yc+1,$xc+2,$yc+$d_tx-1,$door_color);
          $ih->line($xc-2,$yc+$d_tx,$xc+1,$yc+$d_tx,$door_color);
      } else {
        my $xc = int(($x1 + $x2) / 2);

          $ih->line($xc-$d_tx,$yc-2,$xc-$d_tx,$yc+1,$door_color);
        $ih->line($xc-$d_tx+1,$yc+2,$xc-1,$yc+2,$door_color);
          $ih->line($xc,$yc-1,$xc,$yc+1,$door_color);
            $ih->line($xc+1,$yc-2,$xc+$d_tx-1,$yc-2,$door_color);
          $ih->line($xc+$d_tx,$yc-1,$xc+$d_tx,$yc+2,$door_color);
      }
    }
    if ($attr->{'arch'}) {
      if ($xc) {
        $ih->filledRectangle($xc-1,$y1,$xc+1,$y1+$a_px,$arch_color);
        $ih->filledRectangle($xc-1,$y2-$a_px,$xc+1,$y2,$arch_color);
      } else {
        $ih->filledRectangle($x1,$yc-1,$x1+$a_px,$yc+1,$arch_color);
        $ih->filledRectangle($x2-$a_px,$yc-1,$x2,$yc+1,$arch_color);
      }
    }
    if ($attr->{'door'}) {
      if ($xc) {
        $ih->rectangle($xc-$d_tx,  $y1+$a_px+1,
                       $xc+$d_tx,$y2-$a_px-1,$door_color);
      } else {
        $ih->rectangle($x1+$a_px+1,$yc-$d_tx,
                       $x2-$a_px-1,$yc+$d_tx,$door_color);
      }
    }
    if ($attr->{'lock'}) {
      if ($xc) {
        $ih->line($xc,$y1+$a_px+1,$xc,$y2-$a_px-1,$door_color);
      } else {
        $ih->line($x1+$a_px+1,$yc,$x2-$a_px-1,$yc,$door_color);
      }
    }
    if ($attr->{'trap'}) {
      if ($xc) {
        my $yc = int(($y1 + $y2) / 2);
        $ih->line($xc-$t_tx,$yc,$xc+$t_tx,$yc,$door_color);
      } else {
        my $xc = int(($x1 + $x2) / 2);
        $ih->line($xc,$yc-$t_tx,$xc,$yc+$t_tx,$door_color);
      }
    }
    if ($attr->{'portc'}) {
      if ($xc) {
        my $y; for ($y = $y1+$a_px+2; $y < $y2-$a_px; $y += 2) {
          $ih->setPixel($xc,$y,$door_color);
        }
      } else {
        my $x; for ($x = $x1+$a_px+2; $x < $x2-$a_px; $x += 2) {
          $ih->setPixel($x,$yc,$door_color);
        }
      }
    }
  }
  return $ih;
  }

  # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
  # door attributes

  sub door_attr {
  my ($door) = @_;
  my $attr;

  if ($door->{'key'} eq 'arch') {
    $attr->{'arch'} = 1;
  } elsif ($door->{'key'} eq 'open') {
    $attr->{'arch'} = 1; $attr->{'door'} = 1;
  } elsif ($door->{'key'} eq 'lock') {
    $attr->{'arch'} = 1; $attr->{'door'} = 1; $attr->{'lock'} = 1;
  } elsif ($door->{'key'} eq 'trap') {
    $attr->{'arch'} = 1; $attr->{'door'} = 1; $attr->{'trap'} = 1;
    $attr->{'lock'} = 1 if ($door->{'desc'} =~ /Lock/);
  } elsif ($door->{'key'} eq 'secret') {
    $attr->{'wall'} = 1; $attr->{'arch'} = 1, $attr->{'secret'} = 1;
  } elsif ($door->{'key'} eq 'portc') {
    $attr->{'arch'} = 1; $attr->{'portc'} = 1;
  }
  return $attr;
  }
  */
    // HashSet<string> door_attr(DoorData door) => door.key switch
    // {
    //     //   if ($door->{'key'} eq 'arch') {
    //     //     $attr->{'arch'} = 1;
    //     "arch" => ["arch"],
    //     //   } elsif ($door->{'key'} eq 'open') {
    //     //     $attr->{'arch'} = 1; $attr->{'door'} = 1;
    //     "open" => ["arch", "door"],
    //     //   } elsif ($door->{'key'} eq 'lock') {
    //     //     $attr->{'arch'} = 1; $attr->{'door'} = 1; $attr->{'lock'} = 1;
    //     "lock" => ["arch", "door", "lock"],
    //     //   } elsif ($door->{'key'} eq 'trap') {
    //     //     $attr->{'arch'} = 1; $attr->{'door'} = 1; $attr->{'trap'} = 1;
    //     //     $attr->{'lock'} = 1 if ($door->{'desc'} =~ /Lock/);
    //     "trap" => new(Enumerable.Concat(
    //         ["arch", "door", "trap"], (door.desc?.Contains("Lock") ?? false) ? ["lock"] : [])),
    //     //   } elsif ($door->{'key'} eq 'secret') {
    //     //     $attr->{'wall'} = 1; $attr->{'arch'} = 1, $attr->{'secret'} = 1;
    //     "secret" => ["wall", "arch", "secret"],
    //     //   } elsif ($door->{'key'} eq 'portc') {
    //     //     $attr->{'arch'} = 1; $attr->{'portc'} = 1;
    //     "portc" => ["arch", "portc"],
    //     _ => throw new InvalidDataException($"{door.key} is not a recognized key"),
    // };
    /*

    # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # #
    # image labels

    sub image_labels {
    my ($dungeon,$image,$ih) = @_;
    my $cell = $dungeon->{'cell'};
    my $dim = $image->{'cell_size'};
    my $pal = $image->{'palette'};
    my $color = &get_color($pal,'label');

    my $r; for ($r = 0; $r <= $dungeon->{'n_rows'}; $r++) {
      my $c; for ($c = 0; $c <= $dungeon->{'n_cols'}; $c++) {
        next unless ($cell->[$r][$c] & $OPENSPACE);

        my $char = &cell_label($cell->[$r][$c]);
           next unless (defined $char);
        my $x = ($c * $dim) + $image->{'char_x'};
        my $y = ($r * $dim) + $image->{'char_y'};

        $ih->string($image->{'font'},$x,$y,$char,$color);
      }
    }
    return $ih;
    }

    # - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    # cell label

    sub cell_label {
    my ($cell) = @_;
    my $i = ($cell >> 24) & 0xFF;
       return unless ($i);
    my $char = chr($i);
       return unless ($char =~ /^\d/);
    return $char;
    }

    # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # #
    # image stairs

    sub image_stairs {
    my ($dungeon,$image,$ih) = @_;
    my $list = $dungeon->{'stair'};
       return $ih unless ($list);
    my $dim = $image->{'cell_size'};
    my $s_px = int($dim / 2);
    my $t_px = int($dim / 20) + 2;
    my $pal = $image->{'palette'};
    my $color = &get_color($pal,'stair');

    my $stair; foreach $stair (@{ $list }) {
      if ($stair->{'next_row'} > $stair->{'row'}) {
        my $xc = int(($stair->{'col'} + 0.5) * $dim);
        my $y1 = $stair->{'row'} * $dim;
        my $y2 = ($stair->{'next_row'} + 1) * $dim;

        my $y; for ($y = $y1; $y < $y2; $y += $t_px) {
          my $dx; if ($stair->{'key'} eq 'down') {
            $dx = int((($y - $y1) / ($y2 - $y1)) * $s_px);
          } else {
            $dx = $s_px;
          }
          $ih->line($xc-$dx,$y,$xc+$dx,$y,$color);
        }
      } elsif ($stair->{'next_row'} < $stair->{'row'}) {
        my $xc = int(($stair->{'col'} + 0.5) * $dim);
        my $y1 = ($stair->{'row'} + 1) * $dim;
        my $y2 = $stair->{'next_row'} * $dim;

        my $y; for ($y = $y1; $y > $y2; $y -= $t_px) {
          my $dx; if ($stair->{'key'} eq 'down') {
            $dx = int((($y - $y1) / ($y2 - $y1)) * $s_px);
          } else {
            $dx = $s_px;
          }
          $ih->line($xc-$dx,$y,$xc+$dx,$y,$color);
        }
      } elsif ($stair->{'next_col'} > $stair->{'col'}) {
        my $x1 = $stair->{'col'} * $dim;
        my $x2 = ($stair->{'next_col'} + 1) * $dim;
        my $yc = int(($stair->{'row'} + 0.5) * $dim);

        my $x; for ($x = $x1; $x < $x2; $x += $t_px) {
          my $dy; if ($stair->{'key'} eq 'down') {
            $dy = int((($x - $x1) / ($x2 - $x1)) * $s_px);
          } else {
            $dy = $s_px;
          }
          $ih->line($x,$yc-$dy,$x,$yc+$dy,$color);
        }
      } elsif ($stair->{'next_col'} < $stair->{'col'}) {
        my $x1 = ($stair->{'col'} + 1) * $dim;
        my $x2 = $stair->{'next_col'} * $dim;
        my $yc = int(($stair->{'row'} + 0.5) * $dim);

        my $x; for ($x = $x1; $x > $x2; $x -= $t_px) {
          my $dy; if ($stair->{'key'} eq 'down') {
            $dy = int((($x - $x1) / ($x2 - $x1)) * $s_px);
          } else {
            $dy = $s_px;
          }
          $ih->line($x,$yc-$dy,$x,$yc+$dy,$color);
        }
      }
    }
    return $ih;
    }
      */
    #endregion Not-Yet-Converted-Perl
}
