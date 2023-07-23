namespace ArgsParser.Attributes;

public interface IArgAttribute
{
    public bool Required { get; set; }
    public string? GroupName { get; set; }
}
