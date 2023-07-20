namespace ArtHoarderCore.Serializable;

internal class ConstantsJson
{
    public string ApplicationName { get; set; } = null!;
    public string PerceptualHashingLibs { get; set; } = null!;
    public string ArchiveMainFileName { get; init; } = null!;
    public string MetaFilesDirectory { get; init; } = null!;
    public string MainDBName { get; init; } = null!;
    public string PHashDbsDirectory { get; init; } = null!;
    public string DownloadedMediaDirectory { get; init; } = null!;
    public string LogDirectory { get; init; } = null!;
    public string ParsersConfigsDirectory { get; init; } = null!;
    public string ChangesAuditDbName { get; set; } = null!;
    public string DefaultOtherDirectory { get; set; } = null!;
}
