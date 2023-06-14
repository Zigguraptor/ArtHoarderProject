using System.Linq.Expressions;
using ArtHoarderCore.DAL.Entities;
using ArtHoarderCore.Infrastructure;

namespace ArtHoarderCore.DAL;

public interface IDataBaseController
{
    public delegate Task UpdaterAsync(Uri uri, string ownerName);

    string[] PHashAlgorithmsList { get; }

    Task<Dictionary<string, byte[]>> GetFilesXxHashes();
    GalleryProfile? GetGalleryProfile(Uri uri);
    Submission? GetSubmission(Uri uri);

    void UpdateGalleryProfile(GalleryProfile galleryProfile);
    void UpdateSubmission(Submission submission);
    FileMetaInfo? FindFileByHash(byte[] xxHash);

    // public bool HashExists(byte[] xxHash, out Guid? guid);
    bool TryAddGalleryProfile(GalleryProfile galleryProfile);
    void AddNewFileMetaInfo(FileMetaInfo fileMetaInfo);
    void AddNewSubmission(Submission submission);
    bool TryAddNewUser(string name);
    bool TryAddGalleryProfile(Uri uri, string ownerName);
    bool GalleryExists(Uri uri);
    Task UpdateGalleries(UpdaterAsync updaterAsync);
    public List<FullSubmissionInfo> GetFullSubmissionInfos(Expression<Func<FullSubmissionInfo, object>> order);
    public List<FullSubmissionInfo> GetFullSubmissionInfos(Expression<Func<FullSubmissionInfo, bool>> where);

    public List<FullSubmissionInfo> GetFullSubmissionInfos(Expression<Func<FullSubmissionInfo, bool>> where,
        Expression<Func<FullSubmissionInfo, object>> order);

    public List<FullSubmissionInfo> GetFullSubmissionInfos(Expression<Func<FullSubmissionInfo, bool>> where,
        string hashName);

    public List<FullSubmissionInfo> GetFullSubmissionInfos(string hashName);

    string[] GetUsers();
    List<ProfileInfo> GetProfiles();
    public List<ProfileInfo> GetProfiles(Expression<Func<ProfileInfo, bool>> where);

    // public List<FullSubmissionInfo> GetSubmissionsByHash(string hashName, string hash, int range);
    List<FileMetaInfo> GetFiles(IEnumerable<string> paths);
    public List<FileMetaInfo> GetFilesInfo(Expression<Func<FileMetaInfo, bool>> where);
    void AddSubmissionFile(SubmissionFileMetaInfo submissionFileMetaInfo);
}