using ArtHoarderArchiveService.Archive.DAL.Entities;

namespace ArtHoarderArchiveService.Archive.Infrastructure;

public interface IFileHandler
{
    FileMetaInfo SaveFileIfNotExists(Stream fileStream, string workDirectory, string? localDirectoryName,
        string fileName, CancellationToken cancellationToken);
}
