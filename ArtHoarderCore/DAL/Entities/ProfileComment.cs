using System.ComponentModel.DataAnnotations.Schema;

namespace ArtHoarderCore.DAL.Entities;

public class ProfileComment
{
    [ForeignKey("GalleryProfileUri")] public GalleryProfile GalleryProfile { get; set; } = null!;
    public Uri GalleryProfileUri { get; set; } = null!;
}