using ArtHoarderCore.DAL.Entities;

namespace ArtHoarderCore.Parsers;

internal interface IParsHandler
{
    Logger Logger { get; }

    bool RegisterGalleryProfile(GalleryProfile galleryProfile, string? saveFolder);
    void RegisterSubmission(ParsedSubmission? parsedSubmission, string? saveFolder);
}
