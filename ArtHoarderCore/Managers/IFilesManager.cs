using System.Net.Http.Headers;
using ArtHoarderCore.DAL.Entities;
using ArtHoarderCore.Infrastructure;

namespace ArtHoarderCore.Managers;

public interface IFilesManager
{
    string WorkDirectory { get; }
    List<IImageHasher> ImageHashMakers { get; }

    HashSet<string> UnregisteredFiles { get; }
    HashSet<string> MissingFiles { get; }
    HashSet<string> ChangedFiles { get; }

    Task ValidateFilesAsync(Dictionary<string, byte[]> filesHashes, ProgressReporter reporter);
    // ValueTask RegisterFile(string path);

    Task<List<(FileMetaInfo fileMetaInfo, Uri fileUri, HttpHeaders httpHeaders)>> CheckOrSaveFilesAsync(
        string? localDirectoryName, List<Uri> uris);

    Task<(FileMetaInfo fileMetaInfo, Uri fileUri, HttpHeaders httpHeaders)> CheckOrSaveFileAsync(
        string? localDirectoryName, Uri uri);
}