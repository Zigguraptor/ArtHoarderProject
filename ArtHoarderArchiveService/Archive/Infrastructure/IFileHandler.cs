using ArtHoarderArchiveService.Archive.DAL.Entities;

namespace ArtHoarderArchiveService.Archive.Infrastructure;

public interface IFileHandler
{
    FileMetaInfo SaveFileIfNotExists(ReadOnlySpan<byte> readOnlySpan, string workDirectory, string? relativeDirectoryName,
        string fileName, CancellationToken cancellationToken);
}
