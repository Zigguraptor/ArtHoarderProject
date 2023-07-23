namespace ArgsParser.Attributes;

public class OptionAttribute : Attribute, IArgAttribute
{
    private readonly string _name;

    public OptionAttribute(string name, bool required = false)
    {
        _name = name;
        Required = required;
    }

    public bool Required { get; set; }
    public string? GroupName { get; set; }
}
