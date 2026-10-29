using System.Globalization;

namespace RonSijm.IJzer.IaC.Serialization;

internal sealed class ArchitectureElementWriter(XElement element)
{
    public void WriteRequired(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"The required '{name}' value on <{element.Name.LocalName}> is empty.");
        }

        element.SetAttributeValue(name, value);
    }

    public void WriteOptional(string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            element.SetAttributeValue(name, value);
        }
    }

    public void WriteOptionalValue(string name, string? value)
    {
        if (value is not null)
        {
            element.SetAttributeValue(name, value);
        }
    }

    public void WriteOptional<T>(string name, T? value) where T : struct
    {
        if (value.HasValue)
        {
            element.SetAttributeValue(name, Format(value.Value));
        }
    }

    public void WriteValue<T>(string name, T value) where T : struct
    {
        element.SetAttributeValue(name, Format(value));
    }

    public void WriteWhenTrue(string name, bool value)
    {
        if (value)
        {
            element.SetAttributeValue(name, "true");
        }
    }

    public void WriteBoolean(string name, bool value)
    {
        element.SetAttributeValue(name, value ? "true" : "false");
    }

    public void WriteList<T>(string name, IEnumerable<T> values)
    {
        var formatted = values.Select(Format).ToArray();
        if (formatted.Length > 0)
        {
            element.SetAttributeValue(name, string.Join(", ", formatted));
        }
    }

    public static string Format<T>(T value)
    {
        var result = value switch
        {
            bool boolean => boolean ? "true" : "false",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value?.ToString() ?? string.Empty
        };

        return result;
    }
}
