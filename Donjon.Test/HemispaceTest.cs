
using Donjon.Original;
using Donjon.Test.Utilities;

using Microsoft.Extensions.Logging;

using SixLabors.ImageSharp;

using Xunit.Abstractions;

namespace Donjon.Test;

public class HemispaceTest(ITestOutputHelper outputHelper) : HostedTestBase<HemispaceTest>(outputHelper)
{
    [Theory]
    [InlineData("od ev", 3, 4)]
    [InlineData("od od", 3, 5)]
    [InlineData("ev od", 2, 3)]
    [InlineData("ev ev", 2, 4)]
    public void ReconstitutePoint(string reason, int x, int y)
    {
        var fmt = (Rectangle r) => new { r.X, r.Y, r.Width, r.Height, r.Bottom, r.Right, r.Top, r.Left };
        var pfmt = (Point r) => new { r.X, r.Y };

        Logger.LogInformation("{reason} Given <{x},{y}>", reason, x, y);
        Realspace<Point> p_real = new(new(x, y));
        Hemispace<Point> p_hemi = p_real.ToHemi();
        Logger.LogInformation("hemipt = {b}", pfmt(p_hemi.Value));
        Realspace<Point> p_after = p_hemi.ToRealspace();
        Logger.LogInformation("\n{a} \n=?\n {b}", pfmt(p_real), pfmt(p_after));
        Assert.Equal(p_real, p_after);
    }

    [Theory]
    [InlineData("odPt-evSz", 3, 3, 4, 4)]
    [InlineData("odPt-odSz", 3, 3, 5, 5)]
    [InlineData("evPt-odSz", 2, 2, 3, 3)]
    [InlineData("evPt-evSz", 2, 2, 4, 4)]
    [InlineData("odev-odev", 3, 4, 5, 6)]
    [InlineData("odod-evod", 3, 3, 6, 5)]
    [InlineData("evod-odev", 2, 3, 5, 6)]
    [InlineData("evev-odev", 2, 4, 6, 5)]
    public void ReconstituteRect(string reason, int x, int y, int width, int height)
    {
        var fmt = (Rectangle r) => new { r.X, r.Y, r.Width, r.Height, r.Bottom, r.Right, r.Top, r.Left };
        var pfmt = (Point r) => new { r.X, r.Y };


        Logger.LogInformation("{reason} Given <{x},{y}> @ w{w} * h{h}", reason, x, y, width, height);
        // Given
        Realspace<Rectangle> real = new(new(x, y, width, height));
        Hemispace<Rectangle> hemi = real.ToHemi();
        Logger.LogInformation("hemi = {b}", fmt(hemi.Value));
        Realspace<Rectangle> after = hemi.ToRealspace();
        Logger.LogInformation("\nbefore{a} ->\n  hemi{h} ->\n after{b}", fmt(real), fmt(hemi.Value), fmt(after));
        Assert.Equal(real, after);
        Logger.LogInformation("--------------------");

        Realspace<Point> p_real = new(new(x, y));
        Hemispace<Point> p_hemi = p_real.ToHemi();
        Logger.LogInformation("hemipt = {b}", pfmt(p_hemi.Value));
        Realspace<Point> p_after = p_hemi.ToRealspace();
        Logger.LogInformation("\n{a} \n=?\n {b}", pfmt(p_real), pfmt(p_after));
        Assert.Equal(p_real, p_after);
    }

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
        Point p = new(x: 1, y: 2);
        Point p2 = new(x: 1 + 3, y: 2 + 4);
        // Then
        Assert.True(r.Contains(p));
        Logger.LogInformation("pos is in");
        Assert.False(r.Contains(p2));
        Logger.LogInformation("ext is out");
        // Really, P2 refers to the cell that STARTS FROM the rectangle's far corner.
        // Length and Width properties are like a collection Length/Count

        Assert.True(r.Contains(new Point(x: 1 + 0, y: 2 + 1)));
    }
}