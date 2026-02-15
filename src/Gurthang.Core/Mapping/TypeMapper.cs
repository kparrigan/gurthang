namespace Gurthang.Core.Mapping;

public static class TypeMapper
{
    public static string MapType(string? openApiType, string? format, string? itemsType = null, string? itemsFormat = null, string? refName = null)
    {
        if (refName is not null)
            return NameHelper.ToClassName(refName);

        return openApiType?.ToLowerInvariant() switch
        {
            "string" => MapStringType(format),
            "integer" => MapIntegerType(format),
            "number" => MapNumberType(format),
            "boolean" => "bool",
            "array" => $"List<{MapType(itemsType, itemsFormat, refName: null)}>",
            "object" => "Dictionary<string, object>",
            _ => "object"
        };
    }

    public static string MapStringType(string? format)
    {
        return format?.ToLowerInvariant() switch
        {
            "date-time" => "DateTimeOffset",
            "date" => "DateOnly",
            "uuid" => "Guid",
            "uri" => "Uri",
            "byte" => "byte[]",
            "binary" => "Stream",
            _ => "string"
        };
    }

    public static string MapIntegerType(string? format)
    {
        return format?.ToLowerInvariant() switch
        {
            "int64" => "long",
            _ => "int"
        };
    }

    public static string MapNumberType(string? format)
    {
        return format?.ToLowerInvariant() switch
        {
            "float" => "float",
            _ => "double"
        };
    }

    public static bool IsNumericType(string csharpType)
    {
        return csharpType is "int" or "long" or "float" or "double" or "decimal";
    }

    public static bool IsValueType(string csharpType)
    {
        return csharpType is "int" or "long" or "float" or "double" or "bool"
            or "DateTimeOffset" or "DateOnly" or "Guid" or "decimal";
    }

    public static string MakeNullable(string csharpType, bool isRequired)
    {
        if (!isRequired && IsValueType(csharpType))
            return csharpType + "?";
        return csharpType;
    }
}
