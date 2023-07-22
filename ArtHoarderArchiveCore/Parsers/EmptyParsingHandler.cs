using ArtHoarderCore.DAL.Entities;

namespace ArtHoarderCore.Parsers;

public class EmptyParsingHandler : IParsHandler
{
    public Logger Logger => null!;

    public bool RegisterGalleryProfile(GalleryProfile galleryProfile, string? saveFolder)
    {
        throw new Exception("Этот метод не должен быть вызван. Но кто то его вызвал.");
    }

    public void RegisterSubmission(ParsedSubmission? parsedSubmission, string? saveFolder)
    {
        throw new Exception("Этот метод не должен быть вызван. Но кто то его вызвал.");
    }
}
