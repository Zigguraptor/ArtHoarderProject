using System.ComponentModel.DataAnnotations.Schema;

namespace ArtHoarderCore.DAL.Entities;

public class ProfileInfo
{
    public string OwnerName { get; set; }
    public string? UserName { get; set; }
    public Uri Uri { get; set; }

    [NotMapped] public bool? IsChecked { get; set; } = false;
    [NotMapped] public string Resource => Uri.Host;
}