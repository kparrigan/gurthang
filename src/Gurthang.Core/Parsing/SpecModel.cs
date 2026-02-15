namespace Gurthang.Core.Parsing;

public record ParsedSpec
{
    public string Title { get; init; } = "Api";
    public string? Description { get; init; }
    public string? Version { get; init; }
    public string? BaseUrl { get; init; }
    public List<ParsedModel> Models { get; init; } = [];
    public List<ParsedEnum> Enums { get; init; } = [];
    public List<ParsedOperation> Operations { get; init; } = [];
    public List<ParsedSecurityScheme> SecuritySchemes { get; init; } = [];
}

public record ParsedModel
{
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public string? ParentName { get; init; }
    public List<ParsedProperty> Properties { get; init; } = [];
    public PolymorphismInfo? Polymorphism { get; init; }
}

public record ParsedProperty
{
    public string Name { get; init; } = "";
    public string JsonName { get; init; } = "";
    public string CSharpType { get; init; } = "object";
    public bool IsRequired { get; init; }
    public bool IsNullable { get; init; }
    public string? Description { get; init; }
    public string? RefName { get; init; }
    public int? MinLength { get; init; }
    public int? MaxLength { get; init; }
    public string? Pattern { get; init; }
    public decimal? Minimum { get; init; }
    public decimal? Maximum { get; init; }
    public string? Example { get; init; }
}

public record ParsedEnum
{
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public List<ParsedEnumValue> Values { get; init; } = [];
}

public record ParsedEnumValue
{
    public string Name { get; init; } = "";
    public string OriginalValue { get; init; } = "";
}

public record ParsedOperation
{
    public string OperationId { get; init; } = "";
    public string Tag { get; init; } = "Default";
    public string HttpMethod { get; init; } = "GET";
    public string Path { get; init; } = "/";
    public string? Summary { get; init; }
    public string? Description { get; init; }
    public string ReturnType { get; init; } = "void";
    public bool ReturnsList { get; init; }
    public List<ParsedParameter> Parameters { get; init; } = [];
    public ParsedRequestBody? RequestBody { get; init; }
}

public record ParsedParameter
{
    public string Name { get; init; } = "";
    public string CSharpName { get; init; } = "";
    public string CSharpType { get; init; } = "string";
    public string Location { get; init; } = "query"; // path, query, header, cookie
    public bool IsRequired { get; init; }
    public string? Description { get; init; }
    public string? Example { get; init; }
}

public record ParsedRequestBody
{
    public string CSharpType { get; init; } = "object";
    public string ContentType { get; init; } = "application/json";
    public bool IsRequired { get; init; }
    public string? Description { get; init; }
}

public record ParsedSecurityScheme
{
    public string Name { get; init; } = "";
    public SecuritySchemeType Type { get; init; }
    public string? Scheme { get; init; } // bearer, basic
    public string? ApiKeyName { get; init; }
    public string? ApiKeyLocation { get; init; } // header, query, cookie
}

public enum SecuritySchemeType
{
    HttpBearer,
    HttpBasic,
    ApiKey,
    OAuth2,
    OpenIdConnect
}

public record PolymorphismInfo
{
    public string DiscriminatorPropertyName { get; init; } = "";
    public List<DerivedTypeMapping> DerivedTypes { get; init; } = [];
}

public record DerivedTypeMapping
{
    public string DiscriminatorValue { get; init; } = "";
    public string TypeName { get; init; } = "";
}
