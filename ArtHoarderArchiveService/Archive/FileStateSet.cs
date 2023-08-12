using System.Text;

namespace ArtHoarderArchiveService.Archive;

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

    public override string ToString()
    {
        var sb = new StringBuilder();
        if (UnregisteredFiles.Count > 0)
        {
            sb.Append("Unregistered files:\n");
            foreach (var fileName in UnregisteredFiles)
                sb.Append(fileName);
        }
        else
        {
            sb.Append("No unregistered files.");
        }

        if (MissingFiles.Count > 0)
        {
            sb.Append("missing files:\n");
            foreach (var fileName in MissingFiles)
                sb.Append(fileName);
        }
        else
        {
            sb.Append("No missing files.");
        }

        if (ChangedFiles.Count > 0)
        {
            sb.Append("Changed files:\n");
            foreach (var fileName in ChangedFiles)
                sb.Append(fileName);
        }
        else
        {
            sb.Append("No changed files.");
        }

        return sb.ToString();
    }
}
