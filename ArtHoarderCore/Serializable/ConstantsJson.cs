namespace ArtHoarderCore.Serializable;

internal class ConstantsJson
{
    public string ArchiveMainFileName { get; init; }
    public string MetaFilesDirectory { get; init; }
    public string MainDBName { get; init; }
    public string PHashDbsDirectory { get; init; }
    public string DownloadedMediaDirectory { get; init; }
    public string SQLFilesDirectory { get; init; }
    public string LogDirectory { get; init; }
    public string ParsersConfigsDirectory { get; init; }
    public string ChangesAuditDbName { get; set; }
    public string DefaultOtherDirectory { get; set; }
}
