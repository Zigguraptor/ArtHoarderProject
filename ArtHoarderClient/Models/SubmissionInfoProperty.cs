namespace ArtHoarderClient.Models;

public class SubmissionInfoProperty
{
    public SubmissionInfoProperty(string propertyName, string? value)
    {
        PropertyName = propertyName;
        Value = value;
    }

    public string PropertyName { get; set; }
    public string? Value { get; set; }
}