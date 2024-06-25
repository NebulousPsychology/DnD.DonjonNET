using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
namespace Donjon.ImageTools;
public static class ImageSharpExtensions
{

    public static Point Position(this Rectangle value) => new(value.X, value.Y);
    public static Size Size(this Rectangle value) => new(value.Width, value.Height);
    public static Point LocalCenter(this Rectangle value) => (Point)(value.Size() / 2);

    public static Point Center(this Rectangle value) => value.LocalCenter() + (Size)value.Position();

    // /// <summary>
    // /// so that the original can be reused
    // /// </summary>
    // /// <param name="source"></param>
    // /// <param name="r"></param>
    // /// <param name="p"></param>
    // /// <param name="degrees"></param>
    // /// <returns></returns>
    //     static object ClonedRotate(Image source, Rectangle r, Point p, float degrees)
    //     {
    //         Image image = source.Clone(x => x
    //             .Rotate(degrees) // rotates CW around center
    //             .Resize()
    //         );
    //         return image;
    //     }
}