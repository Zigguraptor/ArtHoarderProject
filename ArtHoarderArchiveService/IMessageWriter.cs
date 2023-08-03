namespace ArtHoarderArchiveService;

public interface IMessageWriter
{
    public void Write(string message);
    public void Rewrite(string line);
}
