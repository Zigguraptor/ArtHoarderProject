namespace ArtHoarderArchive;

public class ProgressBar
{
    public string Name { get; set; } = null!;
    public int Max { get; set; }
    public int Current { get; set; } = 0;
    public string Msg { get; set; } = string.Empty;
    public List<ProgressBar> SubBars { get; } = new();
}
