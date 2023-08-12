using ArtHoarderArchiveService.Archive.DAL.Entities;
using ArtHoarderArchiveService.PipeCommunications;

namespace ArtHoarderArchiveService.Archive.Infrastructure;

public interface IUniversalParser
{
    public Task LightUpdateGalleryAsync
        (IProgressWriter progressWriter, Uri galleryUri, string directoryName, CancellationToken cancellationToken);

    public List<Uri>? GetSubscriptions(Uri uri, CancellationToken cancellationToken);
    public string? TryGetUserName(Uri uri);

    public Task ScheduledUpdateGalleryAsync(IProgressWriter progressWriter,
        ScheduledGalleryUpdateInfo scheduledGalleryUpdateInfo, CancellationToken cancellationToken,
        string directoryName);
}
