using Gurthang.Core.Mapping;
using Gurthang.Core.Parsing;

namespace Gurthang.Core.Generation;

public class SolutionGenerator
{
    public void Generate(ParsedSpec spec, string outputDir)
    {
        var solutionName = NameHelper.SolutionNameFromTitle(spec.Title);
        var clientProjectName = $"{solutionName}.Client";
        var testProjectName = $"{solutionName}.Tests";
        var demoProjectName = $"{solutionName}.Demo";
        var clientNamespace = clientProjectName;
        var testNamespace = testProjectName;

        var clientDir = Path.Combine(outputDir, "src", clientProjectName);
        var testDir = Path.Combine(outputDir, "src", testProjectName);
        var demoDir = Path.Combine(outputDir, "src", demoProjectName);

        // Generate models
        var modelGen = new ModelGenerator();
        var modelFiles = modelGen.Generate(spec, clientNamespace);
        WriteFiles(clientDir, modelFiles);

        // Generate auth/infrastructure
        var authGen = new AuthGenerator();
        var authFiles = authGen.Generate(spec, clientNamespace, testProjectName);
        WriteFiles(clientDir, authFiles);

        // Generate API clients
        var apiClientGen = new ApiClientGenerator();
        var apiClientFiles = apiClientGen.Generate(spec, clientNamespace);
        WriteFiles(clientDir, apiClientFiles);

        // Generate tests
        var testGen = new TestGenerator();
        var testFiles = testGen.Generate(spec, clientNamespace, testNamespace);
        WriteFiles(testDir, testFiles);

        // Generate demo
        var demoGen = new DemoGenerator();
        var demoContent = demoGen.Generate(spec, clientNamespace);
        WriteFile(Path.Combine(demoDir, "Program.cs"), demoContent);

        // Generate project files
        var projGen = new ProjectFileGenerator();
        WriteFile(Path.Combine(clientDir, $"{clientProjectName}.csproj"),
            projGen.GenerateClientProject(clientNamespace));
        WriteFile(Path.Combine(testDir, $"{testProjectName}.csproj"),
            projGen.GenerateTestProject(clientProjectName));
        WriteFile(Path.Combine(demoDir, $"{demoProjectName}.csproj"),
            projGen.GenerateDemoProject(clientProjectName));

        // Generate solution file
        var projects = new List<SolutionProject>
        {
            new(clientProjectName, $"src\\{clientProjectName}\\{clientProjectName}.csproj"),
            new(testProjectName, $"src\\{testProjectName}\\{testProjectName}.csproj"),
            new(demoProjectName, $"src\\{demoProjectName}\\{demoProjectName}.csproj")
        };
        WriteFile(Path.Combine(outputDir, $"{solutionName}.sln"),
            projGen.GenerateSolution(solutionName, projects));
    }

    private static void WriteFiles(string baseDir, Dictionary<string, string> files)
    {
        foreach (var (relativePath, content) in files)
        {
            WriteFile(Path.Combine(baseDir, relativePath), content);
        }
    }

    private static void WriteFile(string path, string content)
    {
        var dir = Path.GetDirectoryName(path);
        if (dir != null)
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, content);
    }
}
