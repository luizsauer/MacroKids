using MacroKids.Core.Serialization;
using MacroKids.Core.Services;

namespace MacroKids.Core.Tests;

public class PinValueReaderTests
{
    [Theory]
    [InlineData(-1446, 435, true)]
    [InlineData(-1623, 223, true)]
    [InlineData(500, 300, true)]
    [InlineData(-1, -1, false)]
    [InlineData(0, 0, true)]
    public void HasExplicitCoordinates_handles_multi_monitor_values(int x, int y, bool expected)
    {
        Assert.Equal(expected, PinValueReader.HasExplicitCoordinates(x, y));
    }

    [Fact]
    public async Task Mkproject_click_coordinates_are_negative_on_multi_monitor()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "testeclicks.mkproject"));

        if (!File.Exists(path))
            return;

        var doc = await ProjectPackager.UnpackAsync(path);
        var click = doc.Nodes.First(n => n.TypeId == "mouse.left_click");

        Assert.True(click.PinValues.TryGetValue("x", out var x));
        Assert.True(click.PinValues.TryGetValue("y", out var y));
        Assert.True(PinValueReader.TryConvertToInt(x!, out int xInt));
        Assert.True(PinValueReader.TryConvertToInt(y!, out int yInt));
        Assert.True(xInt < 0);
        Assert.True(PinValueReader.HasExplicitCoordinates(xInt, yInt));
    }
}
