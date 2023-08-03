using System.Globalization;
using ArtHoarderArchiveService.Archive.Managers;

namespace ArtHoarderArchiveService.Archive.DAL.Entities;

public class FullSubmissionInfo
{
    private readonly Submission _submission;

    public FullSubmissionInfo(string ownerName,
        string? profileName, Submission submission, List<FileMetaInfo> fileMetaInfo)
    {
        OwnerName = ownerName;
        ProfileName = profileName;
        _submission = submission;
        SubmissionFiles = fileMetaInfo;
    }

    public string SubmissionUri => _submission.Uri.ToString();
    public string OwnerName { get; }
    public string? ProfileName { get; }
    public string? Title => _submission.Title;
    public string? Description => _submission.Description;
    public string? Tags => _submission.Tags;

    [NotDisplay] public List<FileMetaInfo> SubmissionFiles { get; }
    public int FilesCount => SubmissionFiles.Count;

    public string? PublicationTime => _submission.PublicationTime.ToString();
    public string FirstSaveSubmissionTime => _submission.FirstSaveTime.ToString(CultureInfo.InvariantCulture);
    public string LastUpdateTime => _submission.LastUpdateTime.ToString(CultureInfo.InvariantCulture);
    [NotDisplay] public string IconPath => SubmissionFiles.FirstOrDefault()?.IconPath ?? Constants.UnknownFileIconPath;

    [NotDisplay]
    public Property[] Properties
    {
        get
        {
            var type = GetType();
            var displayProps = type.GetProperties().Where(prop => !prop.IsDefined(typeof(NotDisplayAttribute), true))
                .ToArray();
            var result = new Property[displayProps.Length];
            for (var i = 0; i < displayProps.Length; i++)
            {
                result[i] = new Property(displayProps[i].Name, displayProps[i].GetValue(this)?.ToString());
            }

            return result;
        }
    }
}