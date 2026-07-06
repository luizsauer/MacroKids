using MacroKids.Core.Services;

namespace MacroKids.Core.Tests;

public class KeyboardMapperTests
{
    [Theory]
    [InlineData("W+A", 2)]
    [InlineData("w+a", 2)]
    [InlineData("WA", 2)]
    [InlineData("wa", 2)]
    [InlineData("W, A", 2)]
    [InlineData("Ctrl+Shift+A", 3)]
    public void ParseKeyTokens_supports_multi_key_expressions(string input, int expectedCount)
    {
        var keys = KeyboardMapper.ParseKeyTokens(input);
        Assert.Equal(expectedCount, keys.Count);
    }

    [Fact]
    public void ParseKeyTokens_wa_resolves_to_w_and_a()
    {
        var keys = KeyboardMapper.ParseKeyTokens("WA");
        Assert.Equal(0x57, keys[0]);
        Assert.Equal(0x41, keys[1]);
    }
}
