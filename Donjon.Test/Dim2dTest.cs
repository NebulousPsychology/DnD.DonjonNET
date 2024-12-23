using Donjon;

using Microsoft.Extensions.Logging;

using SixLabors.ImageSharp;

using Xunit.Abstractions;
namespace Donjon.Test;
public class Dim2dTest(ITestOutputHelper output) : Utilities.HostedTestBase<Dim2dTest>(output)
{
    public static IEnumerable<object[]> RectCells { get; } = [
        [new Rectangle(1,1,1,1), new (int,int)[]{ (1, 1) }],
        [new Rectangle(x:3,y:4,width:2,height:3), new (int,int)[]{ (4,3),(4,4), (5,3),(5,4), (6,3),(6,4),}],
        [new Rectangle(x:6, y:8, width:2,height:2), new (int,int)[]{ (8,6),(8,7),(9,6),(9,7) }],
        [new Rectangle(1,1,1,height:4), new (int,int)[]{ (1, 1),(2,1),(3,1),(4,1) }],
        // [new Rectangle(1,1,0,height:4), new (int,int)[]{ (1, 1),(2,1),(3,1),(4,1) }],// throws
        // [new Rectangle(), new (int,int)[]{ (1, 2) }],
        ];

    [Theory, MemberData(nameof(RectCells))]
    public void RectangleEnumeration(Rectangle r, IEnumerable<(int r, int c)> expected)
    {
        Logger.LogInformation("re {v}", new{r.X,r.Y,r.Height,r.Width,r.Top,r.Left,r.Bottom,r.Right});
        // Given
        (int r, int c)[] actual = [.. Dim2d.RangeInclusive(r).OrderBy(a => a.c).OrderBy(a => a.r)];
        Assert.Equal(expected.Count(), actual.Length);
        (int r, int c)[] exOrdered = [.. expected.OrderBy(a => a.c).OrderBy(a => a.r)];

        // Then
        Logger.LogInformation("ex {v}", string.Join(",", exOrdered));
        Logger.LogInformation("ac {v}", string.Join(",", actual));
        Assert.Equal(string.Join(",", exOrdered), string.Join(",", actual));
        Assert.Equal(exOrdered, actual);
    }

}