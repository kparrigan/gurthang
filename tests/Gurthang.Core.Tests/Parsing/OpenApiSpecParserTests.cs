using Gurthang.Core.Parsing;
using Xunit;

namespace Gurthang.Core.Tests.Parsing;

public class OpenApiSpecParserTests
{
    private readonly ParsedSpec _spec;

    public OpenApiSpecParserTests()
    {
        var parser = new OpenApiSpecParser();
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "petstore.yaml");
        _spec = parser.Parse(fixturePath);
    }

    [Fact]
    public void Parse_ExtractsTitle()
    {
        Assert.Equal("Petstore", _spec.Title);
    }

    [Fact]
    public void Parse_ExtractsBaseUrl()
    {
        Assert.Equal("https://petstore.example.com/v1", _spec.BaseUrl);
    }

    [Fact]
    public void Parse_ExtractsModels()
    {
        Assert.Equal(2, _spec.Models.Count);
        Assert.Contains(_spec.Models, m => m.Name == "Pet");
        Assert.Contains(_spec.Models, m => m.Name == "NewPet");
    }

    [Fact]
    public void Parse_PetModel_HasCorrectProperties()
    {
        var pet = _spec.Models.First(m => m.Name == "Pet");
        Assert.Equal(4, pet.Properties.Count);

        var idProp = pet.Properties.First(p => p.Name == "Id");
        Assert.Equal("long", idProp.CSharpType);
        Assert.True(idProp.IsRequired);

        var nameProp = pet.Properties.First(p => p.Name == "Name");
        Assert.Equal("string", nameProp.CSharpType);
        Assert.True(nameProp.IsRequired);

        var tagProp = pet.Properties.First(p => p.Name == "Tag");
        Assert.False(tagProp.IsRequired);
    }

    [Fact]
    public void Parse_ExtractsEnums()
    {
        Assert.Single(_spec.Enums);
        var status = _spec.Enums[0];
        Assert.Equal("PetStatus", status.Name);
        Assert.Equal(3, status.Values.Count);
        Assert.Contains(status.Values, v => v.OriginalValue == "available");
        Assert.Contains(status.Values, v => v.OriginalValue == "pending");
        Assert.Contains(status.Values, v => v.OriginalValue == "sold");
    }

    [Fact]
    public void Parse_ExtractsOperations()
    {
        Assert.Equal(5, _spec.Operations.Count);
    }

    [Fact]
    public void Parse_ListPetsOperation()
    {
        var op = _spec.Operations.First(o => o.OperationId == "ListPets");
        Assert.Equal("pets", op.Tag);
        Assert.Equal("GET", op.HttpMethod);
        Assert.Equal("/pets", op.Path);
        Assert.Equal("Pet", op.ReturnType);
        Assert.True(op.ReturnsList);
        Assert.Single(op.Parameters);
        Assert.Equal("limit", op.Parameters[0].Name);
    }

    [Fact]
    public void Parse_CreatePetOperation_HasRequestBody()
    {
        var op = _spec.Operations.First(o => o.OperationId == "CreatePet");
        Assert.Equal("POST", op.HttpMethod);
        Assert.NotNull(op.RequestBody);
        Assert.Equal("NewPet", op.RequestBody.CSharpType);
        Assert.True(op.RequestBody.IsRequired);
    }

    [Fact]
    public void Parse_DeletePet_ReturnsVoid()
    {
        var op = _spec.Operations.First(o => o.OperationId == "DeletePet");
        Assert.Equal("DELETE", op.HttpMethod);
        Assert.Equal("void", op.ReturnType);
    }

    [Fact]
    public void Parse_ExtractsSecuritySchemes()
    {
        Assert.Equal(2, _spec.SecuritySchemes.Count);
        Assert.Contains(_spec.SecuritySchemes, s => s.Type == SecuritySchemeType.HttpBearer);
        Assert.Contains(_spec.SecuritySchemes, s => s.Type == SecuritySchemeType.ApiKey && s.ApiKeyName == "X-API-Key");
    }
}
