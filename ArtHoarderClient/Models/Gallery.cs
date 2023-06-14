using System;

namespace ArtHoarderClient.Models;

public class Gallery
{
    public Gallery(Uri galleryProfileUri, string? ownerName)
    {
        GalleryProfileUri = galleryProfileUri;
        OwnerName = ownerName;
    }

    public Uri GalleryProfileUri { get; set; }
    public string? OwnerName { get; set; }
}