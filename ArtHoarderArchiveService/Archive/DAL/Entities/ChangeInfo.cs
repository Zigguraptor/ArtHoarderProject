using System.ComponentModel.DataAnnotations;

namespace ArtHoarderArchiveService.Archive.DAL.Entities;

public class ChangeInfo
{
    [Key] public int Id { get; set; }

    [Required] public DateTime ChangeTime { get; set; }
    [Required] public string TableName { get; set; } = null!;
    [Required] public string ColumnName { get; set; } = null!;
    [Required] public string PrimaryKey { get; set; } = null!;
    public string? OldData { get; set; }
}