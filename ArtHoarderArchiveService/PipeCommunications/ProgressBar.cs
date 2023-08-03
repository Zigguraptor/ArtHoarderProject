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

    public void UpdateBar(IEnumerable<string> name, string msg)
    {
        var enumerator = name.GetEnumerator();
        enumerator.Reset();
        if (!enumerator.MoveNext()) return;
        if (Name != enumerator.Current) return;

        var progressBar = this;
        if (enumerator.MoveNext())
            progressBar = Find(enumerator);

        if (progressBar == null) return;
        progressBar.Current++;
        progressBar.Msg = msg;
    }

    private ProgressBar? Find(IEnumerator<string> enumerator)
    {
        foreach (var progressBar in SubBars)
        {
            if (progressBar.Name != enumerator.Current) continue;
            if (enumerator.MoveNext())
            {
                progressBar.Find(enumerator);
            }
            else
            {
                return progressBar;
            }
        }

        return null;
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
        if (enumerator.MoveNext() && enumerator.MoveNext())
            Delete(enumerator);
    }
}
