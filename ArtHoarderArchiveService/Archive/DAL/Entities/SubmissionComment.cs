using System.ComponentModel.DataAnnotations.Schema;

namespace ArtHoarderArchiveService.Archive.DAL.Entities;

public class SubmissionComment
{
    [ForeignKey("SubmissionUri")] public Submission Submission { get; set; } = null!;
    public Uri SubmissionUri { get; set; } = null!;
}
