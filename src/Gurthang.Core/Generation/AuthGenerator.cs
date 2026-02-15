using Gurthang.Core.Parsing;

namespace Gurthang.Core.Generation;

public class AuthGenerator
{
    private readonly TemplateRenderer _renderer = new();

    public Dictionary<string, string> Generate(ParsedSpec spec, string rootNamespace, string testProjectName)
    {
        var files = new Dictionary<string, string>();

        var hasBearer = spec.SecuritySchemes.Any(s =>
            s.Type is SecuritySchemeType.HttpBearer or SecuritySchemeType.OAuth2);
        var hasBasic = spec.SecuritySchemes.Any(s => s.Type == SecuritySchemeType.HttpBasic);
        var hasApiKey = spec.SecuritySchemes.Any(s => s.Type == SecuritySchemeType.ApiKey);
        var apiKeyScheme = spec.SecuritySchemes.FirstOrDefault(s => s.Type == SecuritySchemeType.ApiKey);

        // ClientConfiguration
        var configModel = new
        {
            Namespace = rootNamespace,
            Title = spec.Title,
            DefaultBaseUrl = spec.BaseUrl,
            HasBearer = hasBearer,
            HasBasic = hasBasic,
            HasApiKey = hasApiKey,
            ApiKeyHeaderName = apiKeyScheme?.ApiKeyName ?? "X-API-Key"
        };
        files["ClientConfiguration.cs"] = _renderer.Render("ClientConfiguration", configModel);

        // BaseApiClient
        var baseClientModel = new
        {
            Namespace = rootNamespace,
            TestProjectName = testProjectName,
            HasBearer = hasBearer,
            HasBasic = hasBasic,
            HasApiKey = hasApiKey
        };
        files["BaseApiClient.cs"] = _renderer.Render("BaseApiClient", baseClientModel);

        // ApiException
        var exceptionModel = new { Namespace = rootNamespace };
        files["ApiException.cs"] = _renderer.Render("ApiException", exceptionModel);

        return files;
    }
}
