namespace ArtHoarderArchiveService.PipeCommunications;

public class ProgressBar
{
    public ProgressBar(string name, byte max)
    {
        Name = name;
        Max = max;
    }

    public string Name { get; }
    public int Max { get; }
    public int Current { get; set; } = 0;
    public string Msg { get; set; } = string.Empty;
    public List<ProgressBar> SubBars { get; } = new();

    public void AddSubBar(string parent, ProgressBar progressBar)
    {
        Find(parent)?.SubBars.Add(progressBar);
    }

    public void UpdateBar(string name, string msg)
    {
        var progressBar = Find(name);
        if (progressBar == null) return;
        progressBar.Current++;
        progressBar.Msg = msg;
    }

    private ProgressBar? Find(string name)
    {
        if (Name == name) return this;
        return SubBars.Select(progressBar => progressBar.Find(name)).FirstOrDefault(bar => bar != null);
    }

    private void Delete(IEnumerator<string> enumerator)
    {
        foreach (var progressBar in SubBars)
        {
            if (progressBar.Name != enumerator.Current) continue;

            if (enumerator.MoveNext())
            {
                progressBar.Delete(enumerator);
            }
            else
            {
                SubBars.Remove(progressBar);
                return;
            }
        }
    }

    public void Delete(IEnumerable<string> name)
    {
        var enumerator = name.GetEnumerator();
        enumerator.Reset();
        if (enumerator.MoveNext())
            Delete(enumerator);
    }
}
