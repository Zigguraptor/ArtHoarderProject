using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ArtHoarderCore.DAL.Entities;

[PrimaryKey(nameof(SubmissionUri), nameof(FileGuid))]
public class SubmissionFileMetaInfo
{
    public SubmissionFileMetaInfo(Uri submissionUri, Guid fileGuid, Uri fileUri)
    {
        SubmissionUri = submissionUri;
        FileGuid = fileGuid;
        FileUri = fileUri;
    }

    public Uri SubmissionUri { get; set; }
    public Guid FileGuid { get; set; }
    public Uri FileUri { get; set; }

    [ForeignKey("SubmissionUri")] public Submission Submission { get; set; } = null!;
    [ForeignKey("FileGuid")] public FileMetaInfo FileMetaInfo { get; set; } = null!;
}