using System;
using System.Collections.Generic;
using ArtHoarderCore.Infrastructure;
using ArtHoarderCore.Managers;

namespace ArtHoarderClient.Infrastructure.FoldersAndItems;

public class CustomFolderNode : FolderNode
{
    public List<Uri> SubmissionUris { get; set; } = new();
    public List<Guid> FilesGuids { get; set; } = new();

    public CustomFolderNode(string title, BaseNode? parent = null) : base(title, parent)
    {
    }

    public override List<IDisplayViewItem> GetViewItems(Archive archive)
    {
        if (Children.Count == 0)
            Refresh(archive);
        return base.GetViewItems(archive);
    }

    protected override void Refresh(Archive archive)
    {
        Children.Clear();
        foreach (var submissionInfo in archive.GetSubmissions(info => SubmissionUris.Contains(info.Uri)))
            Children.Add(new SubmissionNode(submissionInfo, this));

        foreach (var fileMetaInfo in archive.GetFilesInfo(info => FilesGuids.Contains(info.Guid)))
            Children.Add(new FileNode(fileMetaInfo, this));
    }
}