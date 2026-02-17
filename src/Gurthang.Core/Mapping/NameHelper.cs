using System.Text;
using System.Text.RegularExpressions;

namespace Gurthang.Core.Mapping;

public static partial class NameHelper
{
    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char",
        "checked", "class", "const", "continue", "decimal", "default", "delegate", "do",
        "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
        "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int",
        "interface", "internal", "is", "lock", "long", "namespace", "new", "null",
        "object", "operator", "out", "override", "params", "private", "protected",
        "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof",
        "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
        "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
        "virtual", "void", "volatile", "while"
    };

    public static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sb = new StringBuilder();
        bool capitalizeNext = true;

        foreach (char c in input)
        {
            if (c == '_' || c == '-' || c == ' ' || c == '.')
            {
                capitalizeNext = true;
                continue;
            }

            if (capitalizeNext)
            {
                sb.Append(char.ToUpperInvariant(c));
                capitalizeNext = false;
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    public static string ToCamelCase(string input)
    {
        var pascal = ToPascalCase(input);
        if (pascal.Length == 0)
            return pascal;

        return char.ToLowerInvariant(pascal[0]) + pascal[1..];
    }

    public static string ToClassName(string input)
    {
        var name = ToPascalCase(SanitizeIdentifier(input));
        if (name.Length == 0)
            return "Unknown";

        if (char.IsDigit(name[0]))
            name = "_" + name;

        return name;
    }

    public static string ToPropertyName(string input)
    {
        return ToClassName(input);
    }

    public static string ToParameterName(string input)
    {
        var name = ToCamelCase(SanitizeIdentifier(input));
        if (name.Length == 0)
            return "value";

        if (char.IsDigit(name[0]))
            name = "_" + name;

        if (CSharpKeywords.Contains(name))
            name = "@" + name;

        return name;
    }

    public static string ToEnumMemberName(string input)
    {
        var name = ToPascalCase(SanitizeIdentifier(input));
        if (name.Length == 0)
            return "Unknown";

        if (char.IsDigit(name[0]))
            name = "_" + name;

        return name;
    }

    public static string ToSafeFileName(string input)
    {
        return InvalidFileCharsRegex().Replace(input, "");
    }

    public static string SanitizeIdentifier(string input)
    {
        return NonIdentifierCharsRegex().Replace(input, "_");
    }

    public static string ToApiClientClassName(string tag)
    {
        var name = ToPascalCase(tag);
        if (!name.EndsWith("Api", StringComparison.Ordinal))
            name += "Api";
        return name;
    }

    /// <summary>
    /// Sanitizes a string for use inside an XML doc comment line.
    /// Replaces newlines with spaces and escapes XML special characters.
    /// </summary>
    public static string? ToXmlDocSafe(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Replace newlines with spaces to keep it on one line
        var result = input
            .Replace("\r\n", " ")
            .Replace("\r", " ")
            .Replace("\n", " ");

        // Collapse multiple spaces
        result = MultipleSpacesRegex().Replace(result, " ").Trim();

        // Escape XML special characters
        result = result
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");

        return result;
    }

    public static string SolutionNameFromTitle(string title)
    {
        var name = ToPascalCase(title);
        return ToSafeFileName(name);
    }

    [GeneratedRegex(@"[^a-zA-Z0-9_]")]
    private static partial Regex NonIdentifierCharsRegex();

    [GeneratedRegex(@"[^\w\-.]")]
    private static partial Regex InvalidFileCharsRegex();

    [GeneratedRegex(@" {2,}")]
    private static partial Regex MultipleSpacesRegex();
}
