using Gurthang.Core.Generation;
using Gurthang.Core.Parsing;
using Xunit;

namespace Gurthang.Core.Tests.Generation;

public class SolutionGeneratorTests : IDisposable
{
    private readonly string _outputDir;

    public SolutionGeneratorTests()
    {
        _outputDir = Path.Combine(Path.GetTempPath(), $"gurthang-test-{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        if (Directory.Exists(_outputDir))
            Directory.Delete(_outputDir, true);
    }

    [Fact]
    public void Generate_PetstoreSpec_CreatesExpectedFiles()
    {
        // Arrange
        var parser = new OpenApiSpecParser();
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "petstore.yaml");
        var spec = parser.Parse(fixturePath);

        var generator = new SolutionGenerator();

        // Act
        generator.Generate(spec, _outputDir);

        // Assert - solution file
        Assert.True(File.Exists(Path.Combine(_outputDir, "Petstore.sln")));

        // Assert - client project
        var clientDir = Path.Combine(_outputDir, "src", "Petstore.Client");
        Assert.True(File.Exists(Path.Combine(clientDir, "Petstore.Client.csproj")));
        Assert.True(File.Exists(Path.Combine(clientDir, "ClientConfiguration.cs")));
        Assert.True(File.Exists(Path.Combine(clientDir, "BaseApiClient.cs")));
        Assert.True(File.Exists(Path.Combine(clientDir, "ApiException.cs")));

        // Assert - models
        Assert.True(File.Exists(Path.Combine(clientDir, "Models", "Pet.cs")));
        Assert.True(File.Exists(Path.Combine(clientDir, "Models", "NewPet.cs")));
        Assert.True(File.Exists(Path.Combine(clientDir, "Models", "PetStatus.cs")));

        // Assert - API client interfaces
        Assert.True(File.Exists(Path.Combine(clientDir, "Api", "IPetsApi.cs")));
        Assert.True(File.Exists(Path.Combine(clientDir, "Api", "IStoreApi.cs")));

        // Assert - API clients
        Assert.True(File.Exists(Path.Combine(clientDir, "Api", "PetsApi.cs")));
        Assert.True(File.Exists(Path.Combine(clientDir, "Api", "StoreApi.cs")));

        // Assert - test project
        var testDir = Path.Combine(_outputDir, "src", "Petstore.Tests");
        Assert.True(File.Exists(Path.Combine(testDir, "Petstore.Tests.csproj")));
        Assert.True(File.Exists(Path.Combine(testDir, "Api", "PetsApiTests.cs")));
        Assert.True(File.Exists(Path.Combine(testDir, "Api", "StoreApiTests.cs")));

        // Assert - demo project
        var demoDir = Path.Combine(_outputDir, "src", "Petstore.Demo");
        Assert.True(File.Exists(Path.Combine(demoDir, "Petstore.Demo.csproj")));
        Assert.True(File.Exists(Path.Combine(demoDir, "Program.cs")));
    }

    [Fact]
    public void Generate_PetstoreSpec_ModelContainsExpectedContent()
    {
        var parser = new OpenApiSpecParser();
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "petstore.yaml");
        var spec = parser.Parse(fixturePath);

        var generator = new SolutionGenerator();
        generator.Generate(spec, _outputDir);

        var petContent = File.ReadAllText(
            Path.Combine(_outputDir, "src", "Petstore.Client", "Models", "Pet.cs"));
        Assert.Contains("public class Pet", petContent);
        Assert.Contains("[JsonPropertyName(\"id\")]", petContent);
        Assert.Contains("public long Id", petContent);
        Assert.Contains("public string Name", petContent);
        Assert.Contains("[Required]", petContent);
        Assert.Contains("using System.ComponentModel.DataAnnotations;", petContent);
        Assert.Contains("[StringLength(100, MinimumLength = 1)]", petContent);
        Assert.Contains("[RegularExpression", petContent);
    }

    [Fact]
    public void Generate_PetstoreSpec_ApiClientContainsExpectedContent()
    {
        var parser = new OpenApiSpecParser();
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "petstore.yaml");
        var spec = parser.Parse(fixturePath);

        var generator = new SolutionGenerator();
        generator.Generate(spec, _outputDir);

        var petsApiContent = File.ReadAllText(
            Path.Combine(_outputDir, "src", "Petstore.Client", "Api", "PetsApi.cs"));
        Assert.Contains("public class PetsApi : BaseApiClient, IPetsApi", petsApiContent);
        Assert.Contains("ListPetsAsync", petsApiContent);
        Assert.Contains("CreatePetAsync", petsApiContent);
        Assert.Contains("ShowPetByIdAsync", petsApiContent);
        Assert.Contains("DeletePetAsync", petsApiContent);
    }

    [Fact]
    public void Generate_PetstoreSpec_ClientConfigHasSecuritySchemes()
    {
        var parser = new OpenApiSpecParser();
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "petstore.yaml");
        var spec = parser.Parse(fixturePath);

        var generator = new SolutionGenerator();
        generator.Generate(spec, _outputDir);

        var configContent = File.ReadAllText(
            Path.Combine(_outputDir, "src", "Petstore.Client", "ClientConfiguration.cs"));
        Assert.Contains("BearerToken", configContent);
        Assert.Contains("ApiKey", configContent);
        Assert.Contains("X-API-Key", configContent);
    }
}
