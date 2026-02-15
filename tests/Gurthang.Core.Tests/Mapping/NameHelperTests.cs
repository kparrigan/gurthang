using Gurthang.Core.Mapping;
using Xunit;

namespace Gurthang.Core.Tests.Mapping;

public class NameHelperTests
{
    [Theory]
    [InlineData("hello_world", "HelloWorld")]
    [InlineData("some-api-name", "SomeApiName")]
    [InlineData("already PascalCase", "AlreadyPascalCase")]
    [InlineData("with.dots", "WithDots")]
    [InlineData("ALL_CAPS", "ALLCAPS")]
    [InlineData("", "")]
    public void ToPascalCase_ConvertsCorrectly(string input, string expected)
    {
        Assert.Equal(expected, NameHelper.ToPascalCase(input));
    }

    [Theory]
    [InlineData("HelloWorld", "helloWorld")]
    [InlineData("some-api", "someApi")]
    [InlineData("", "")]
    public void ToCamelCase_ConvertsCorrectly(string input, string expected)
    {
        Assert.Equal(expected, NameHelper.ToCamelCase(input));
    }

    [Theory]
    [InlineData("User", "User")]
    [InlineData("order-item", "OrderItem")]
    [InlineData("123start", "_123start")]
    [InlineData("my type!", "MyType")]
    public void ToClassName_SanitizesCorrectly(string input, string expected)
    {
        Assert.Equal(expected, NameHelper.ToClassName(input));
    }

    [Theory]
    [InlineData("user_id", "userId")]
    [InlineData("class", "@class")]
    [InlineData("return", "@return")]
    [InlineData("normalName", "normalName")]
    public void ToParameterName_HandlesKeywords(string input, string expected)
    {
        Assert.Equal(expected, NameHelper.ToParameterName(input));
    }

    [Theory]
    [InlineData("users", "UsersApi")]
    [InlineData("PetsApi", "PetsApi")]
    [InlineData("store-operations", "StoreOperationsApi")]
    public void ToApiClientClassName_AppendsApiSuffix(string input, string expected)
    {
        Assert.Equal(expected, NameHelper.ToApiClientClassName(input));
    }

    [Theory]
    [InlineData("Acme Store", "AcmeStore")]
    [InlineData("My Cool API", "MyCoolAPI")]
    public void SolutionNameFromTitle_RemovesSpaces(string input, string expected)
    {
        Assert.Equal(expected, NameHelper.SolutionNameFromTitle(input));
    }
}
