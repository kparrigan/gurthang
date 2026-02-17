using Gurthang.Core.Mapping;
using Gurthang.Core.Parsing;

namespace Gurthang.Core.Generation;

public class DemoGenerator
{
    private readonly TemplateRenderer _renderer = new();

    public string Generate(ParsedSpec spec, string clientNamespace)
    {
        var operationsByTag = spec.Operations
            .GroupBy(o => o.Tag)
            .ToDictionary(g => g.Key, g => g.ToList());

        var clients = operationsByTag.Select(kvp =>
        {
            var className = NameHelper.ToApiClientClassName(kvp.Key);
            return new
            {
                ClassName = className,
                VariableName = NameHelper.ToCamelCase(className),
                Operations = kvp.Value.Select(MapDemoOperation).ToList()
            };
        }).ToList();

        var templateModel = new
        {
            ClientNamespace = clientNamespace,
            Title = spec.Title,
            BaseUrl = spec.BaseUrl ?? "https://api.example.com",
            Clients = clients
        };

        return _renderer.Render("DemoProgram", templateModel);
    }

    private static object MapDemoOperation(ParsedOperation op)
    {
        var allParameters = new List<object>();

        foreach (var p in op.Parameters)
        {
            allParameters.Add(new
            {
                p.CSharpName,
                p.CSharpType,
                DefaultValue = p.Example ?? GetDefaultDemoValue(p.CSharpType)
            });
        }

        if (op.RequestBody != null)
        {
            allParameters.Add(new
            {
                CSharpName = "body",
                CSharpType = op.RequestBody.CSharpType,
                DefaultValue = GetDefaultDemoValue(op.RequestBody.CSharpType)
            });
        }

        var returnsVoid = op.ReturnType == "void";
        var fullReturnType = op.ReturnsList ? $"List<{op.ReturnType}>" : op.ReturnType;

        return new
        {
            op.OperationId,
            Summary = NameHelper.ToXmlDocSafe(op.Summary),
            ReturnsVoid = returnsVoid,
            FullReturnType = fullReturnType,
            AllParameters = allParameters,
            HasParameters = allParameters.Count > 0
        };
    }

    private static string GetDefaultDemoValue(string csharpType)
    {
        var baseType = csharpType.TrimEnd('?');
        return baseType switch
        {
            "string" => "\"example\"",
            "int" => "1",
            "long" => "1L",
            "float" => "1.0f",
            "double" => "1.0",
            "bool" => "true",
            "Guid" => "Guid.NewGuid()",
            "DateTimeOffset" => "DateTimeOffset.UtcNow",
            "DateOnly" => "DateOnly.FromDateTime(DateTime.UtcNow)",
            _ when baseType.StartsWith("List<") => $"new {baseType}()",
            _ when baseType.StartsWith("Dictionary<") => $"new {baseType}()",
            _ => $"new {baseType}()"
        };
    }
}
