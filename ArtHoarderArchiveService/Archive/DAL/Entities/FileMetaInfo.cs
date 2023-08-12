using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ArtHoarderArchiveService.Archive.DAL.Entities;

[Index(nameof(LocalFilePath), Name = "LocalFilePath", IsUnique = true)]
[Index(nameof(XxHash), Name = "XxHash", IsUnique = false)]
public class FileMetaInfo
{
    [Key] public Guid Guid { get; set; }
    public string LocalFilePath { get; set; } = null!;
    public byte[] XxHash { get; set; } = null!;
    public DateTime FirstSaveTime { get; set; }

    public ICollection<Submission> Submissions { get; set; } = null!;

    [NotMapped] public string Title => Path.GetFileName(LocalFilePath);
    [NotMapped] [NotDisplay] public string IconPath => LocalFilePath;

    [NotMapped]
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
