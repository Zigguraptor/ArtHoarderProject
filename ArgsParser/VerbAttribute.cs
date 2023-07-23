namespace ArgsParser;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class VerbAttribute : Attribute
{
    public VerbAttribute(string name, bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"No valid verb name. \"{name?.ToString()}\"", nameof(name));
        Name = name;
        IsDefault = isDefault;
    }

    public string Name { get; set; }

    public bool IsDefault { get; set; }

    public string HelpText { get; set; } = string.Empty;
}
