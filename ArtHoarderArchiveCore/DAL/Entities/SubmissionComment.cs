using System.ComponentModel.DataAnnotations.Schema;

namespace ArtHoarderCore.DAL.Entities;

public class SubmissionComment
{
    [ForeignKey("SubmissionUri")] public Submission Submission { get; set; } = null!;
    public Uri SubmissionUri { get; set; } = null!;
}