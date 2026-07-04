namespace NotifyR.Tests;

public class UnitTests
{
    [Fact]
    public void Value_IsSingleton()
    {
        var a = Unit.Value;
        var b = Unit.Value;

        Assert.Equal(a, b);
    }

    [Fact]
    public async Task Completed_ReturnsCompletedTaskWithValue()
    {
        var result = await Unit.Completed;

        Assert.Equal(Unit.Value, result);
    }

    [Fact]
    public void Equals_ReturnsTrueForAnyUnit()
    {
        Assert.True(new Unit().Equals(new Unit()));
        Assert.True(new Unit().Equals((object)new Unit()));
    }

    [Fact]
    public void EqualityOperators_WorkCorrectly()
    {
        Assert.True(Unit.Value == new Unit());
        Assert.False(Unit.Value != new Unit());
    }

    [Fact]
    public void GetHashCode_ReturnsZero()
    {
        Assert.Equal(0, new Unit().GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsUnitSymbol()
    {
        Assert.Equal("()", Unit.Value.ToString());
    }
}
