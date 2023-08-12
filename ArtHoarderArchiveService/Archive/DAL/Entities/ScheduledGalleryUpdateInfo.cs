using System.ComponentModel.DataAnnotations;

namespace ArtHoarderArchiveService.Archive.DAL.Entities;

public class ScheduledGalleryUpdateInfo
{
    [Key] public Uri GalleryUri { get; set; } = null!;
    public DateTime? LastFullUpdate { get; set; }
    public string Host { get; set; } = null!;
    public Uri? FirstLoadedSubmissionUri { get; set; }
    public string? LastLoadedPage { get; set; }
    public int LastLoadedSubmission { get; set; }
}
