
using Donjon.Original;
using Donjon.Test.Utilities;

using Microsoft.Extensions.Logging;

using SixLabors.ImageSharp;

using Xunit.Abstractions;

namespace Donjon.Test;

public class HemispaceTest(ITestOutputHelper outputHelper) : HostedTestBase<HemispaceTest>(outputHelper)
{
    [Fact]
    public void Realspaceifies()
    {
        Dictionary<string, int> proto = new()
        {
            ["i"] = 4, // row (y)
            ["j"] = 3, // col (x)
            ["width"] = 2,
            ["height"] = 3,
        };

        // Given
        var y1 = (proto["i"] * 2) + 1; // 9
        var x1 = (proto["j"] * 2) + 1; // 7
        var y2 = ((proto["i"] + proto["height"]) * 2) - 1; //13 : bottom?
        var x2 = ((proto["j"] + proto["width"]) * 2) - 1; // 9 : right
        // FIXME: the -1 is odd

        Logger.LogInformation("r_expect: <{a},{b},r{c},b{d}> ", x1, y1, x2, y2);
        // When
        Hemispace<Point> p = new(new(proto["j"], proto["i"]));
        Hemispace<Point> p2 = new(new(proto["j"] + proto["width"], proto["i"] + proto["height"]));
        Hemispace<Rectangle> r = new(new(proto["j"], proto["i"], width: proto["width"], height: proto["height"]));
        Logger.LogInformation("r_hemi: <{o}> TLBR:{tlbr}", r.Value, (r.Value.Top, r.Value.Left, r.Value.Bottom, r.Value.Right));

        Point p_real = p.ToRealspace();
        Logger.LogInformation("p_real: <{p}>", p_real);
        Point p2_real = p2.ToRealspace();
        Logger.LogInformation("p2_real: <{p}>", p2_real);
        Rectangle r_real = r.ToRealspace();
        Logger.LogInformation("r_real: <{o}> TLBR:{tlbr}", r_real, (r_real.Top, r_real.Left, r_real.Bottom, r_real.Right));
        // Then
        Assert.Equal(new(x1, y1), p_real);
        Assert.Equal(new(x2, y2), p2_real);
        Assert.Equal([y1, x1, y2, x2], [r_real.Top, r_real.Left, r_real.Bottom, r_real.Right]);
    }

    [Fact]
    public void ContainsAtPerimeter()
    {
        // Given
        Rectangle r = new(x: 1, y: 2, width: 3, height: 4);
        // When
        Point p = new(1, 2);
        Point p2 = new(1+3, 2+4);
        // Then
        Assert.True(r.Contains(p));
        Logger.LogInformation("pos is in");
        Assert.False(r.Contains(p2));
        Logger.LogInformation("ext is out");
    }
}