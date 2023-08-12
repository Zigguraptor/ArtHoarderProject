using System.Net.Http.Headers;
using ArtHoarderArchiveService.Archive.DAL.Entities;

namespace ArtHoarderArchiveService.Archive.Infrastructure;

public interface IFileHandler
{
    public List<(FileMetaInfo fileMetaInfo, Uri fileUri, HttpHeaders httpHeaders)> CheckOrSaveFiles(
        string workDirectory, string? localDirectoryName, List<Uri> uris, CancellationToken cancellationToken);

    public (FileMetaInfo fileMetaInfo, Uri fileUri, HttpHeaders httpHeaders) CheckOrSaveFile(string workDirectory,
        string? localDirectoryName, Uri uri, CancellationToken cancellationToken);
}
