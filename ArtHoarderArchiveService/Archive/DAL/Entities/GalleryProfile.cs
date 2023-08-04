using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArtHoarderArchiveService.Archive.DAL.Entities;

public class GalleryProfile
{
    [Key] public Uri Uri { get; set; } = null!;
    public string ResourceHost { get; set; } = null!;

    public string OwnerName { get; set; } = null!;
    [ForeignKey("OwnerName")] public User Owner { get; set; } = null!; //  foreign key. User.Name
    public string? UserName { get; set; } = null!;
    public DateTime? CreationTime { get; set; }
    public string? Status { get; set; } = null!;
    public string? Description { get; set; } = null!;
    public Uri? IconFileUri { get; set; } = null!;

    public Guid? IconFileGuid { get; set; } //  foreign key. File.Guid

    [ForeignKey("IconFileGuid")] public FileMetaInfo? IconFile { get; set; } = null!; //  foreign key. File.Guid

    public Uri? LastSubmission { get; set; }
    public DateTime FirstSaveTime { get; set; }
    public DateTime LastUpdateTime { get; set; } //any field updated
    public DateTime LastNewUpdateTime { get; set; } // updated all new posts and profile info
    public DateTime LastFullUpdateTime { get; set; } // update all info

    public void Update(GalleryProfile newVersion)
    {
        // if (Uri.ToString() != newVersion.Uri.ToString()) throw new Exception("Attempting to update a gallery with a mismatched link.")

        if (UserName != newVersion.UserName)
            UserName = newVersion.UserName;
        if (CreationTime != newVersion.CreationTime)
            CreationTime = newVersion.CreationTime;
        if (Status != newVersion.Status)
            Status = newVersion.Status;
        if (Description != newVersion.Description)
            Description = newVersion.Description;
        if (IconFileUri?.ToString() != newVersion.IconFileUri?.ToString())
            IconFileUri = newVersion.IconFileUri;
        if (IconFileGuid != newVersion.IconFileGuid)
            IconFileGuid = newVersion.IconFileGuid;
    }
}
