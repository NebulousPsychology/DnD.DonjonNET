namespace Donjon.Test;

using Donjon.Original;
using Donjon.ImageTools;
using Donjon.Test.Utilities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

using Xunit.Abstractions;

public class ImageTest(ITestOutputHelper outputHelper)
:HostedTestBase<ImageTest>(outputHelper)
{
    [Fact]
    public void TestMapImageGeneration()
    {
        // Given
        var opts = Substitute.For<IOptions<ImageMapOptions>>();
        opts.Value.Returns(new ImageMapOptions
        {
            BackgroundFileName = "Images/cell_100x100.png",
            CellTilePath = "Images/cell_100x100.png",
            DoorPath = "Images/door_70x70.png",
            DoorPathArch = "Images/door_arch_70x70.png",
            DoorPathPortc = "Images/door_portc_70x70.png",
            DoorPathSecret = "Images/door_secret_70x70.png",
            DoorPathTrap = "Images/door_trapped_70x70.png",
            PixelsPerTile = 70,
        });
        foreach (var pair in new Dictionary<string, string>
        {
            [nameof(opts.Value.BackgroundFileName)] = opts.Value.BackgroundFileName,
            [nameof(opts.Value.CellTilePath)] = opts.Value.CellTilePath,
            [nameof(opts.Value.DoorPath)] = opts.Value.DoorPath,
            [nameof(opts.Value.DoorPathArch)] = opts.Value.DoorPathArch,
            [nameof(opts.Value.DoorPathPortc)] = opts.Value.DoorPathPortc,
            [nameof(opts.Value.DoorPathSecret)] = opts.Value.DoorPathSecret,
            [nameof(opts.Value.DoorPathTrap)] = opts.Value.DoorPathTrap,
        })
        {
            Assert.True(File.Exists(pair.Value), userMessage: $"{pair.Key} fnf: {pair.Value}");
        }

        // When
        var g = new DungeonGen(LoggerFactory.CreateLogger<DungeonGen>());
        var dungeon = g.Create_dungeon(new Dungeon { seed = 12345 });

        var i = new DungeonImageMapBuilder(LoggerFactory.CreateLogger<DungeonImageMapBuilder>(), opts);
        i.CreateMap(dungeon, "test_CreateMap.jpg", showSecrets: false);
        i.CreateMap(dungeon, "test_CreateMap_secret.jpg", showSecrets: true);

        // Then
    }

    [Theory]
    [InlineData(20)]
    [InlineData(200)]
    [InlineData(02_000)]
    [InlineData(10_000)]
    [InlineData(20_000)]
    public void NormalFuzz(int noise)
    {
        // Given

        // prep output raster image
        using Image<Rgba32> mapImage = new(
            width: 100, height: 100,
            backgroundColor: Color.Gray);

        // prep stamp image, in two colors 
        using Image<Rgba32> errstamp = new(width: 10, height: 10, backgroundColor: Color.Red);
        errstamp.Mutate(x => x.Vignette(radiusX: 2, radiusY: 2));

        using Image<Rgba32> stamp = new(width: 10, height: 10, backgroundColor: Color.White);
        stamp.Mutate(x => x.Vignette(radiusX: 2, radiusY: 2));

        // Random r = new(12345);
        Random r = Random.Shared;
        float imageradius = MathF.Min(mapImage.Bounds.Height, mapImage.Bounds.Width) * 0.5f;
        Assert.Equal(new(100, 100), mapImage.Bounds.Size());
        // Assert.Equal(25, imageradius);

        var pts = Enumerable.Range(0, noise).Select(i =>
                    (Point)((PointF)mapImage.Bounds.Center() + r.NextNormalPointF(imageradius, clamp: true)) //*.25f?
                );
        Assert.NotEqual(1, pts.Distinct().Count());
        float eps = mapImage.Bounds.Width * 0.015f;
        bool inRadius(Point p) => ((Size)(p - (Size)mapImage.Bounds.Center())).Magnitude() <= imageradius + eps;

        // When
        foreach (Point pt in pts)
        {
            bool inImage = mapImage.Bounds.Contains(pt);
            if (!inImage) Logger.LogWarning("{pt} out of image bounds", pt);

            // mapImage.Mutate(x => x.Fill()); // https://docs.sixlabors.com/articles/imagesharp.drawing/gettingstarted.html
            mapImage.Mutate(x => x.DrawImage(inImage ? stamp : errstamp,
                backgroundLocation: pt - (stamp.Bounds.Size() / 2),
                opacity: 0.2f,
                colorBlending: inImage ? (inRadius(pt) ? PixelColorBlendingMode.Add : PixelColorBlendingMode.Subtract) : PixelColorBlendingMode.Add));
        }

        // Then
        string filename = Path.GetFullPath($"NormalFuzz_{noise}.bmp");
        // save
        Logger.LogInformation("Save to {file}", filename);
        mapImage.Save(filename); // Automatic encoder selected based on extension.


        var extents = pts.Aggregate(seed: (minX: 0, maxX: 0, minY: 0, maxY: 0),
        func: (prev, pt) => (
            minX: Math.Min(prev.minX, pt.X), maxX: Math.Max(prev.maxX, pt.X),
            minY: Math.Min(prev.minY, pt.Y), maxY: Math.Max(prev.maxY, pt.Y)
        ));
        Logger.LogInformation("extent:{x}", extents);

        int outofRadius = pts.Count(p => !inRadius(p));
        int outOfImage = pts.Count(p => !mapImage.Bounds.Contains(p));
        Logger.LogInformation("outOfImg:{ooi}, inbounds:{x}, outofbounds:{y}", outOfImage, pts.Count() - outofRadius, outofRadius);
        Assert.Equal(0, outOfImage);
        Assert.Equal(0, outofRadius);

        // Assert.Fail("logs");
    }
}