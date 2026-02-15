using System.Reflection;
using Scriban;
using Scriban.Runtime;

namespace Gurthang.Core.Generation;

public class TemplateRenderer
{
    private readonly Dictionary<string, Template> _cache = new();

    public string Render(string templateName, object model)
    {
        var template = GetTemplate(templateName);
        var scriptObject = new ScriptObject();
        scriptObject.Import(model, renamer: member => member.Name);

        var context = new TemplateContext();
        context.PushGlobal(scriptObject);
        context.MemberRenamer = member => member.Name;

        return template.Render(context);
    }

    private Template GetTemplate(string templateName)
    {
        if (_cache.TryGetValue(templateName, out var cached))
            return cached;

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Gurthang.Core.Templates.{templateName}.sbn";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Template not found: {resourceName}");
        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();
        var template = Template.Parse(text, resourceName);

        if (template.HasErrors)
        {
            var errors = string.Join(Environment.NewLine, template.Messages);
            throw new InvalidOperationException($"Template parse errors in {templateName}:{Environment.NewLine}{errors}");
        }

        _cache[templateName] = template;
        return template;
    }
}
