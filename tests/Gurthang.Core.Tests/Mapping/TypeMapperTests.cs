using Gurthang.Core.Mapping;
using Xunit;

namespace Gurthang.Core.Tests.Mapping;

public class TypeMapperTests
{
    [Theory]
    [InlineData("string", null, "string")]
    [InlineData("string", "date-time", "DateTimeOffset")]
    [InlineData("string", "date", "DateOnly")]
    [InlineData("string", "uuid", "Guid")]
    [InlineData("string", "uri", "Uri")]
    [InlineData("string", "byte", "byte[]")]
    [InlineData("string", "binary", "Stream")]
    [InlineData("integer", null, "int")]
    [InlineData("integer", "int32", "int")]
    [InlineData("integer", "int64", "long")]
    [InlineData("number", null, "double")]
    [InlineData("number", "float", "float")]
    [InlineData("number", "double", "double")]
    [InlineData("boolean", null, "bool")]
    public void MapType_MapsCorrectly(string openApiType, string? format, string expected)
    {
        Assert.Equal(expected, TypeMapper.MapType(openApiType, format));
    }

    [Fact]
    public void MapType_Array_WrapsInList()
    {
        var result = TypeMapper.MapType("array", null, "string", null);
        Assert.Equal("List<string>", result);
    }

    [Fact]
    public void MapType_Ref_ReturnsClassName()
    {
        var result = TypeMapper.MapType(null, null, refName: "Pet");
        Assert.Equal("Pet", result);
    }

    [Theory]
    [InlineData("int", true)]
    [InlineData("long", true)]
    [InlineData("bool", true)]
    [InlineData("string", false)]
    [InlineData("List<int>", false)]
    [InlineData("Uri", false)]
    public void IsValueType_IdentifiesCorrectly(string type, bool expected)
    {
        Assert.Equal(expected, TypeMapper.IsValueType(type));
    }

    [Theory]
    [InlineData("int", false, "int?")]
    [InlineData("int", true, "int")]
    [InlineData("string", false, "string")]
    [InlineData("string", true, "string")]
    public void MakeNullable_HandlesValueAndReferenceTypes(string type, bool required, string expected)
    {
        Assert.Equal(expected, TypeMapper.MakeNullable(type, required));
    }
}
