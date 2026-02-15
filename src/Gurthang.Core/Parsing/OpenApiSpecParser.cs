using Gurthang.Core.Mapping;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace Gurthang.Core.Parsing;

public class OpenApiSpecParser
{
    public ParsedSpec Parse(string specPath)
    {
        using var stream = File.OpenRead(specPath);
        var reader = new OpenApiStreamReader();
        var document = reader.Read(stream, out var diagnostic);

        if (diagnostic.Errors.Any())
        {
            var errors = string.Join(Environment.NewLine, diagnostic.Errors.Select(e => e.Message));
            throw new InvalidOperationException($"OpenAPI spec has errors:{Environment.NewLine}{errors}");
        }

        return ParseDocument(document);
    }

    public ParsedSpec ParseDocument(OpenApiDocument document)
    {
        var spec = new ParsedSpec
        {
            Title = document.Info?.Title ?? "Api",
            Description = document.Info?.Description,
            Version = document.Info?.Version,
            BaseUrl = document.Servers?.FirstOrDefault()?.Url,
            Models = ParseSchemas(document),
            Enums = ParseEnums(document),
            Operations = ParseOperations(document),
            SecuritySchemes = ParseSecuritySchemes(document)
        };

        return spec;
    }

    private List<ParsedModel> ParseSchemas(OpenApiDocument document)
    {
        var models = new List<ParsedModel>();

        if (document.Components?.Schemas == null)
            return models;

        foreach (var (name, schema) in document.Components.Schemas)
        {
            if (schema.Enum?.Any() == true)
                continue; // handled by ParseEnums

            if (schema.Type != "object" && schema.AllOf?.Count == 0)
                continue;

            var model = ParseModelSchema(name, schema);
            models.Add(model);
        }

        return models;
    }

    private ParsedModel ParseModelSchema(string name, OpenApiSchema schema)
    {
        var className = NameHelper.ToClassName(name);
        string? parentName = null;
        var allProperties = new Dictionary<string, OpenApiSchema>();
        var requiredProps = new HashSet<string>(schema.Required ?? Enumerable.Empty<string>());

        // Handle allOf composition
        if (schema.AllOf?.Count > 0)
        {
            foreach (var allOfItem in schema.AllOf)
            {
                if (allOfItem.Reference != null)
                {
                    parentName = NameHelper.ToClassName(allOfItem.Reference.Id);
                }
                else
                {
                    foreach (var (propName, propSchema) in allOfItem.Properties)
                        allProperties[propName] = propSchema;

                    foreach (var req in allOfItem.Required)
                        requiredProps.Add(req);
                }
            }
        }

        // Add direct properties
        foreach (var (propName, propSchema) in schema.Properties)
        {
            allProperties[propName] = propSchema;
        }

        var properties = allProperties.Select(kvp =>
            ParseProperty(kvp.Key, kvp.Value, requiredProps.Contains(kvp.Key))
        ).ToList();

        // Handle polymorphism
        PolymorphismInfo? polymorphism = null;
        if (schema.Discriminator?.PropertyName != null)
        {
            var derivedTypes = new List<DerivedTypeMapping>();
            if (schema.Discriminator.Mapping != null)
            {
                foreach (var (value, refPath) in schema.Discriminator.Mapping)
                {
                    var typeName = NameHelper.ToClassName(refPath.Split('/').Last());
                    derivedTypes.Add(new DerivedTypeMapping
                    {
                        DiscriminatorValue = value,
                        TypeName = typeName
                    });
                }
            }

            polymorphism = new PolymorphismInfo
            {
                DiscriminatorPropertyName = schema.Discriminator.PropertyName,
                DerivedTypes = derivedTypes
            };
        }

        return new ParsedModel
        {
            Name = className,
            Description = schema.Description,
            ParentName = parentName,
            Properties = properties,
            Polymorphism = polymorphism
        };
    }

    private ParsedProperty ParseProperty(string name, OpenApiSchema schema, bool isRequired)
    {
        var (csharpType, refName) = ResolveType(schema);
        var isNullable = schema.Nullable || !isRequired;

        return new ParsedProperty
        {
            Name = NameHelper.ToPropertyName(name),
            JsonName = name,
            CSharpType = TypeMapper.MakeNullable(csharpType, isRequired),
            IsRequired = isRequired,
            IsNullable = isNullable,
            Description = schema.Description,
            RefName = refName,
            MinLength = schema.MinLength.HasValue ? (int)schema.MinLength.Value : null,
            MaxLength = schema.MaxLength.HasValue ? (int)schema.MaxLength.Value : null,
            Pattern = schema.Pattern,
            Minimum = schema.Minimum.HasValue ? (decimal)schema.Minimum.Value : null,
            Maximum = schema.Maximum.HasValue ? (decimal)schema.Maximum.Value : null,
            Example = ConvertOpenApiAny(schema.Example)
        };
    }

    private (string CSharpType, string? RefName) ResolveType(OpenApiSchema schema)
    {
        if (schema.Reference != null)
        {
            var refName = schema.Reference.Id;
            return (NameHelper.ToClassName(refName), refName);
        }

        if (schema.Enum?.Any() == true && schema.Type == "string")
        {
            // Inline enum - will be named by context
            return ("string", null);
        }

        if (schema.Type == "array" && schema.Items != null)
        {
            var (itemType, _) = ResolveType(schema.Items);
            return ($"List<{itemType}>", null);
        }

        if (schema.AdditionalProperties != null)
        {
            var (valueType, _) = ResolveType(schema.AdditionalProperties);
            return ($"Dictionary<string, {valueType}>", null);
        }

        // oneOf / anyOf without discriminator -> JsonElement
        if ((schema.OneOf?.Count > 0 || schema.AnyOf?.Count > 0) && schema.Discriminator == null)
        {
            return ("System.Text.Json.JsonElement?", null);
        }

        return (TypeMapper.MapType(schema.Type, schema.Format), null);
    }

    private List<ParsedEnum> ParseEnums(OpenApiDocument document)
    {
        var enums = new List<ParsedEnum>();

        if (document.Components?.Schemas == null)
            return enums;

        foreach (var (name, schema) in document.Components.Schemas)
        {
            if (schema.Enum?.Any() != true || schema.Type != "string")
                continue;

            var values = schema.Enum
                .OfType<Microsoft.OpenApi.Any.OpenApiString>()
                .Select(e => new ParsedEnumValue
                {
                    Name = NameHelper.ToEnumMemberName(e.Value),
                    OriginalValue = e.Value
                })
                .ToList();

            enums.Add(new ParsedEnum
            {
                Name = NameHelper.ToClassName(name),
                Description = schema.Description,
                Values = values
            });
        }

        return enums;
    }

    private List<ParsedOperation> ParseOperations(OpenApiDocument document)
    {
        var operations = new List<ParsedOperation>();

        if (document.Paths == null)
            return operations;

        foreach (var (path, pathItem) in document.Paths)
        {
            foreach (var (method, operation) in pathItem.Operations)
            {
                var tag = operation.Tags?.FirstOrDefault()?.Name ?? "Default";
                var (returnType, returnsList) = ResolveReturnType(operation);

                var parameters = (operation.Parameters ?? [])
                    .Select(ParseOperationParameter)
                    .ToList();

                // Add path item level parameters
                if (pathItem.Parameters != null)
                {
                    foreach (var param in pathItem.Parameters)
                    {
                        if (parameters.All(p => p.Name != param.Name))
                            parameters.Add(ParseOperationParameter(param));
                    }
                }

                ParsedRequestBody? requestBody = null;
                if (operation.RequestBody != null)
                {
                    requestBody = ParseRequestBody(operation.RequestBody);
                }

                var operationId = operation.OperationId
                    ?? $"{method.ToString().ToLowerInvariant()}_{path.Replace("/", "_").Trim('_')}";

                operations.Add(new ParsedOperation
                {
                    OperationId = NameHelper.ToPascalCase(operationId),
                    Tag = tag,
                    HttpMethod = method.ToString().ToUpperInvariant(),
                    Path = path,
                    Summary = operation.Summary,
                    Description = operation.Description,
                    ReturnType = returnType,
                    ReturnsList = returnsList,
                    Parameters = parameters,
                    RequestBody = requestBody
                });
            }
        }

        return operations;
    }

    private (string ReturnType, bool ReturnsList) ResolveReturnType(OpenApiOperation operation)
    {
        var successResponse = operation.Responses
            ?.Where(r => r.Key.StartsWith("2"))
            .Select(r => r.Value)
            .FirstOrDefault();

        if (successResponse?.Content == null || !successResponse.Content.Any())
            return ("void", false);

        var mediaType = successResponse.Content
            .Where(c => c.Key.Contains("json", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Value)
            .FirstOrDefault();

        if (mediaType?.Schema == null)
            return ("void", false);

        var schema = mediaType.Schema;

        if (schema.Type == "array" && schema.Items != null)
        {
            var (itemType, _) = ResolveType(schema.Items);
            return (itemType, true);
        }

        var (type, _) = ResolveType(schema);
        return (type, false);
    }

    private ParsedParameter ParseOperationParameter(OpenApiParameter param)
    {
        var (csharpType, _) = ResolveType(param.Schema ?? new OpenApiSchema { Type = "string" });

        var example = ConvertOpenApiAny(param.Example)
            ?? ConvertOpenApiAny(param.Schema?.Example);

        return new ParsedParameter
        {
            Name = param.Name,
            CSharpName = NameHelper.ToParameterName(param.Name),
            CSharpType = TypeMapper.MakeNullable(csharpType, param.Required),
            Location = param.In?.ToString()?.ToLowerInvariant() ?? "query",
            IsRequired = param.Required,
            Description = param.Description,
            Example = example
        };
    }

    private ParsedRequestBody? ParseRequestBody(OpenApiRequestBody body)
    {
        if (body.Content == null)
            return null;

        var jsonContent = body.Content
            .Where(c => c.Key.Contains("json", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

        if (jsonContent.Value?.Schema == null)
            return null;

        var (csharpType, _) = ResolveType(jsonContent.Value.Schema);

        return new ParsedRequestBody
        {
            CSharpType = csharpType,
            ContentType = jsonContent.Key,
            IsRequired = body.Required,
            Description = body.Description
        };
    }

    private List<ParsedSecurityScheme> ParseSecuritySchemes(OpenApiDocument document)
    {
        var schemes = new List<ParsedSecurityScheme>();

        if (document.Components?.SecuritySchemes == null)
            return schemes;

        foreach (var (name, scheme) in document.Components.SecuritySchemes)
        {
            var parsed = new ParsedSecurityScheme
            {
                Name = name,
                Type = MapSecuritySchemeType(scheme),
                Scheme = scheme.Scheme,
                ApiKeyName = scheme.Name,
                ApiKeyLocation = scheme.In.ToString()?.ToLowerInvariant()
            };

            schemes.Add(parsed);
        }

        return schemes;
    }

    private SecuritySchemeType MapSecuritySchemeType(OpenApiSecurityScheme scheme)
    {
        return scheme.Type switch
        {
            Microsoft.OpenApi.Models.SecuritySchemeType.Http when
                scheme.Scheme?.Equals("bearer", StringComparison.OrdinalIgnoreCase) == true
                => Parsing.SecuritySchemeType.HttpBearer,
            Microsoft.OpenApi.Models.SecuritySchemeType.Http when
                scheme.Scheme?.Equals("basic", StringComparison.OrdinalIgnoreCase) == true
                => Parsing.SecuritySchemeType.HttpBasic,
            Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
                => Parsing.SecuritySchemeType.ApiKey,
            Microsoft.OpenApi.Models.SecuritySchemeType.OAuth2
                => Parsing.SecuritySchemeType.OAuth2,
            Microsoft.OpenApi.Models.SecuritySchemeType.OpenIdConnect
                => Parsing.SecuritySchemeType.OpenIdConnect,
            _ => Parsing.SecuritySchemeType.HttpBearer
        };
    }

    private static string? ConvertOpenApiAny(IOpenApiAny? value)
    {
        return value switch
        {
            OpenApiString s => $"\"{s.Value}\"",
            OpenApiInteger i => i.Value.ToString(),
            OpenApiLong l => l.Value.ToString(),
            OpenApiFloat f => f.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            OpenApiDouble d => d.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            OpenApiBoolean b => b.Value ? "true" : "false",
            _ => null
        };
    }
}
