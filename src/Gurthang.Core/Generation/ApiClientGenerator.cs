using Gurthang.Core.Mapping;
using Gurthang.Core.Parsing;

namespace Gurthang.Core.Generation;

public class ApiClientGenerator
{
    private readonly TemplateRenderer _renderer = new();

    public Dictionary<string, string> Generate(ParsedSpec spec, string rootNamespace)
    {
        var files = new Dictionary<string, string>();

        var operationsByTag = spec.Operations
            .GroupBy(o => o.Tag)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (tag, operations) in operationsByTag)
        {
            var className = NameHelper.ToApiClientClassName(tag);

            var templateModel = new
            {
                Namespace = rootNamespace,
                Tag = tag,
                ClassName = className,
                Operations = operations.Select(MapOperation).ToList()
            };

            var interfaceContent = _renderer.Render("ApiClientInterface", templateModel);
            files[$"Api/I{className}.cs"] = interfaceContent;

            var content = _renderer.Render("ApiClient", templateModel);
            files[$"Api/{className}.cs"] = content;
        }

        return files;
    }

    private object MapOperation(ParsedOperation op)
    {
        var parameters = op.Parameters.Select(p => new
        {
            p.Name,
            p.CSharpName,
            p.CSharpType,
            p.Location,
            p.IsRequired,
            p.Description
        }).ToList();

        var allParameters = new List<object>(parameters.Cast<object>());
        string? requestBodyParamName = null;

        if (op.RequestBody != null)
        {
            requestBodyParamName = "body";
            allParameters.Add(new
            {
                Name = "body",
                CSharpName = "body",
                CSharpType = op.RequestBody.CSharpType,
                Location = "body",
                IsRequired = op.RequestBody.IsRequired,
                Description = op.RequestBody.Description ?? "Request body"
            });
        }

        var returnsVoid = op.ReturnType == "void";
        var fullReturnType = op.ReturnsList ? $"List<{op.ReturnType}>" : op.ReturnType;

        return new
        {
            op.OperationId,
            HttpMethod = ToRestSharpMethod(op.HttpMethod),
            op.Path,
            op.Summary,
            op.Description,
            ReturnsVoid = returnsVoid,
            FullReturnType = fullReturnType,
            Parameters = parameters,
            AllParameters = allParameters,
            HasParameters = allParameters.Count > 0,
            RequestBody = op.RequestBody != null,
            RequestBodyParamName = requestBodyParamName
        };
    }

    private static string ToRestSharpMethod(string httpMethod)
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
}
