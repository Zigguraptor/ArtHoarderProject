namespace ArtHoarderCore;

public class FileStateSet
{
    public FileStateSet(HashSet<string> unregisteredFiles, HashSet<string> missingFiles,
        HashSet<string> changedFiles)
    {
        UnregisteredFiles = unregisteredFiles;
        MissingFiles = missingFiles;
        ChangedFiles = changedFiles;
    }

    public HashSet<string> UnregisteredFiles { get; }
    public HashSet<string> MissingFiles { get; }
    public HashSet<string> ChangedFiles { get; }
}
