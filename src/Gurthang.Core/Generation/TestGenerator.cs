using Gurthang.Core.Mapping;
using Gurthang.Core.Parsing;

namespace Gurthang.Core.Generation;

public class TestGenerator
{
    private readonly TemplateRenderer _renderer = new();

    public Dictionary<string, string> Generate(ParsedSpec spec, string clientNamespace, string testNamespace)
    {
        var files = new Dictionary<string, string>();

        // Generate shared test helper
        var helperContent = _renderer.Render("TestHelper", new { TestNamespace = testNamespace });
        files["FakeHttpHandler.cs"] = helperContent;

        var operationsByTag = spec.Operations
            .GroupBy(o => o.Tag)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (tag, operations) in operationsByTag)
        {
            var className = NameHelper.ToApiClientClassName(tag);
            var testClassName = $"{className}Tests";

            var templateModel = new
            {
                ClientNamespace = clientNamespace,
                TestNamespace = testNamespace,
                ClassName = className,
                TestClassName = testClassName,
                Operations = operations.Select(MapTestOperation).ToList()
            };

            var content = _renderer.Render("TestClass", templateModel);
            files[$"Api/{testClassName}.cs"] = content;
        }

        return files;
    }

    private object MapTestOperation(ParsedOperation op)
    {
        var allParameters = new List<object>();
        var queryParameters = new List<object>();
        var pathParameters = new List<object>();

        foreach (var p in op.Parameters)
        {
            var testValue = p.Example ?? GetDefaultTestValue(p.CSharpType);
            var param = new
            {
                p.CSharpName,
                p.CSharpType,
                p.Name,
                p.Location,
                DefaultTestValue = testValue
            };
            allParameters.Add(param);
            if (p.Location == "query") queryParameters.Add(param);
            if (p.Location == "path") pathParameters.Add(param);
        }

        if (op.RequestBody != null)
        {
            allParameters.Add(new
            {
                CSharpName = "body",
                CSharpType = op.RequestBody.CSharpType,
                Name = "body",
                Location = "body",
                DefaultTestValue = GetDefaultTestValue(op.RequestBody.CSharpType)
            });
        }

        var returnsVoid = op.ReturnType == "void";
        var fullReturnType = op.ReturnsList ? $"List<{op.ReturnType}>" : op.ReturnType;
        var responseJson = returnsVoid ? "" : (op.ReturnsList ? "[]" : "{}");

        // Build expected path with placeholders resolved
        var expectedPath = op.Path;
        foreach (var p in op.Parameters.Where(p => p.Location == "path"))
        {
            var testValue = p.Example ?? GetDefaultTestValue(p.CSharpType);
            // Strip quotes from string test values for URL
            var urlValue = testValue.Trim('"');
            expectedPath = expectedPath.Replace($"{{{p.Name}}}", urlValue);
        }

        return new
        {
            op.OperationId,
            HttpMethod = ToCSharpHttpMethod(op.HttpMethod),
            Path = op.Path,
            ExpectedPath = expectedPath,
            ReturnsVoid = returnsVoid,
            FullReturnType = fullReturnType,
            AllParameters = allParameters,
            HasParameters = allParameters.Count > 0,
            QueryParameters = queryParameters,
            HasQueryParameters = queryParameters.Count > 0,
            HasRequestBody = op.RequestBody != null,
            ResponseJson = responseJson
        };
    }

    private static string ToCSharpHttpMethod(string httpMethod)
    {
        return httpMethod.ToUpperInvariant() switch
        {
            "GET" => "Get",
            "POST" => "Post",
            "PUT" => "Put",
            "DELETE" => "Delete",
            "PATCH" => "Patch",
            "HEAD" => "Head",
            "OPTIONS" => "Options",
            _ => "Get"
        };
    }

    private static string GetDefaultTestValue(string csharpType)
    {
        var baseType = csharpType.TrimEnd('?');
        return baseType switch
        {
            "string" => "\"test\"",
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
