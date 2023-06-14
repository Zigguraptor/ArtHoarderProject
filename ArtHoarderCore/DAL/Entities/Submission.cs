using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArtHoarderCore.Parsers;

namespace ArtHoarderCore.DAL.Entities;

public class Submission
{
    public Submission()
    {
    }

    public Submission(ParsedSubmission parsedSubmission)
    {
        Uri = parsedSubmission.Uri;
        SourceGalleryUri = parsedSubmission.SourceGalleryUri;
        Title = parsedSubmission.Title;
        Description = parsedSubmission.Description;
        Tags = parsedSubmission.Tags;
        PublicationTime = parsedSubmission.PublicationTime;
    }

    [Key] public Uri Uri { get; set; } = null!;

    public Uri SourceGalleryUri { get; set; } = null!;
    [ForeignKey("SourceGalleryUri")] public GalleryProfile SourceGallery { get; set; } = null!;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Tags { get; set; }

    public DateTime? PublicationTime { get; set; }
    public DateTime FirstSaveTime { get; set; }
    public DateTime LastUpdateTime { get; set; }

    public void Update(ParsedSubmission parsedSubmission)
    {
        if (Title != parsedSubmission.Title)
            Title = parsedSubmission.Title;
        if (Description != parsedSubmission.Description)
            Description = parsedSubmission.Description;
        if (Tags != parsedSubmission.Tags)
            Tags = parsedSubmission.Tags;
        if (PublicationTime != parsedSubmission.PublicationTime)
            PublicationTime = parsedSubmission.PublicationTime;
    }
}