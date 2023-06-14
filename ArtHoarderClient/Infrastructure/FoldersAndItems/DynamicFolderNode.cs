using System;
using System.Collections.Generic;
using System.IO;
using ArtHoarderCore.Infrastructure;
using ArtHoarderCore.Managers;

namespace ArtHoarderClient.Infrastructure.FoldersAndItems;

public class DynamicFolderNode : FolderNode
{
    public override string IconPath => Path.GetFullPath(MyConstants.DynamicFolderIconLocalPath);
    public Action<DynamicFolderNode>? Template { get; set; }

    public DynamicFolderNode(string title, BaseNode? parent = null) : base(title, parent)
    {
    }

    public override List<IDisplayViewItem> GetViewItems(Archive archive)
    {
        if (Template != null && Children.Count == 0)
            Template.Invoke(this);
        return base.GetViewItems(archive);
    }
}