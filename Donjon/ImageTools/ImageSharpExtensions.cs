using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
namespace Donjon.ImageTools;
public static class ImageSharpExtensions
{

    public static Point Position(this Rectangle value) => new(value.X, value.Y);
    public static Size Size(this Rectangle value) => new(value.Width, value.Height);
    public static Point LocalCenter(this Rectangle value) => (Point)(value.Size() / 2);

    public static Point Center(this Rectangle value) => value.LocalCenter() + (Size)value.Position();

    public static double MagnitudeSquared(this Size s) => s.Height * s.Height + s.Width * s.Width;
    public static double Magnitude(this Size s) => Math.Sqrt(s.MagnitudeSquared());

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

    /// <summary>
    /// Produce a point, using box-muller (3 invocations of randomness)
    /// </summary>
    /// <param name="start"></param>
    /// <param name="random"></param>
    /// <see>https://en.wikipedia.org/wiki/Box%E2%80%93Muller_transform</see>
    /// <returns></returns>
    public static PointF NextNormalPointF(this Random random, float scale = 1.0f, bool clamp = false)
    {
        float theta = random.NextSingle() * 2 * MathF.PI;
        float r = MathF.Abs(scale * random.NextNormalF()); // Ensure positive radius
        r = clamp ? r % scale : r; // when clamping, don't pile up in a line at the edge of the radius, restart from 0

        float x = r * MathF.Cos(theta);
        float y = r * MathF.Sin(theta);

        return new PointF(x, y);
    }

    public static float NextNormalF(this Random random)
    {
        float u1 = random.NextSingle();
        float u2 = random.NextSingle();
        float z = MathF.Sqrt(-2 * MathF.Log(u1)) * MathF.Cos(2 * MathF.PI * u2);
        return z;
    }
}