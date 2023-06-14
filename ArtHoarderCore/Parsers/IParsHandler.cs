using ArtHoarderCore.DAL.Entities;

namespace ArtHoarderCore.Parsers;

internal interface IParsHandler
{
    Logger Logger { get; }

    Task<bool> RegisterGalleryProfileAsync(GalleryProfile galleryProfile, string? saveFolder);
    Task RegisterSubmissionAsync(ParsedSubmission? parsedSubmission, string? saveFolder);
}