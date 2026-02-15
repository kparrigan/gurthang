using System.CommandLine;
using Gurthang.Core.Generation;
using Gurthang.Core.Parsing;

var specOption = new Option<FileInfo>(
    aliases: ["--spec", "-s"],
    description: "Path to the OpenAPI spec file (YAML or JSON)")
{ IsRequired = true };

var outputOption = new Option<DirectoryInfo>(
    aliases: ["--output", "-o"],
    description: "Output directory for the generated solution")
{ IsRequired = true };

var rootCommand = new RootCommand("Gurthang — OpenAPI C# Client Generator")
{
    specOption,
    outputOption
};

rootCommand.SetHandler((spec, output) =>
{
    Console.WriteLine($"Parsing spec: {spec.FullName}");

    var parser = new OpenApiSpecParser();
    var parsedSpec = parser.Parse(spec.FullName);

    Console.WriteLine($"  Title: {parsedSpec.Title}");
    Console.WriteLine($"  Models: {parsedSpec.Models.Count}");
    Console.WriteLine($"  Enums: {parsedSpec.Enums.Count}");
    Console.WriteLine($"  Operations: {parsedSpec.Operations.Count}");
    Console.WriteLine($"  Security Schemes: {parsedSpec.SecuritySchemes.Count}");

    Console.WriteLine($"\nGenerating to: {output.FullName}");

    var generator = new SolutionGenerator();
    generator.Generate(parsedSpec, output.FullName);

    Console.WriteLine("Done! Generated solution is ready.");
}, specOption, outputOption);

return await rootCommand.InvokeAsync(args);
