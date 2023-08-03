namespace ArtHoarderArchiveService.Archive.Serializable;

internal class ArchiveMainFile
{
    public string ArchiveRootName { get; init; } = null!;
    public DateTime LastAccess { get; set; }
}
