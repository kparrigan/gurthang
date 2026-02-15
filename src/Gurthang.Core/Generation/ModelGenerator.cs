using Gurthang.Core.Mapping;
using Gurthang.Core.Parsing;

namespace Gurthang.Core.Generation;

public class ModelGenerator
{
    private readonly TemplateRenderer _renderer = new();

    public Dictionary<string, string> Generate(ParsedSpec spec, string rootNamespace)
    {
        var files = new Dictionary<string, string>();

        foreach (var model in spec.Models)
        {
            var templateModel = new
            {
                Namespace = rootNamespace,
                ClassName = model.Name,
                Description = model.Description,
                ParentName = model.ParentName,
                Polymorphism = model.Polymorphism,
                Properties = model.Properties.Select(p => new
                {
                    p.Name,
                    p.JsonName,
                    p.CSharpType,
                    p.IsRequired,
                    p.IsNullable,
                    p.Description,
                    IsValueType = TypeMapper.IsValueType(p.CSharpType.TrimEnd('?')),
                    IsAlreadyNullable = p.CSharpType.EndsWith("?"),
                    p.MinLength,
                    p.MaxLength,
                    p.Pattern,
                    p.Minimum,
                    p.Maximum,
                    IsStringType = p.CSharpType.TrimEnd('?') == "string",
                    IsNumericType = TypeMapper.IsNumericType(p.CSharpType.TrimEnd('?'))
                }).ToList()
            };

            var content = _renderer.Render("Model", templateModel);
            files[$"Models/{model.Name}.cs"] = content;
        }

        foreach (var enumDef in spec.Enums)
        {
            var templateModel = new
            {
                Namespace = rootNamespace,
                EnumName = enumDef.Name,
                Description = enumDef.Description,
                Values = enumDef.Values
            };

            var content = _renderer.Render("Enum", templateModel);
            files[$"Models/{enumDef.Name}.cs"] = content;
        }

        return files;
    }
}
