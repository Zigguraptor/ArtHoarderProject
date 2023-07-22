using System.Collections.Generic;
using System.IO;
using ArtHoarderCore.DAL.Entities;
using ArtHoarderCore.Infrastructure;
using ArtHoarderCore.Managers;

namespace ArtHoarderClient.Infrastructure.FoldersAndItems;

internal class FileNode : BaseNode
{
    public override string IconPath { get; }
    public override bool IsContainer => false;
    public override Property[] Properties { get; }

    public FileNode(FileMetaInfo fileMetaInfo, BaseNode? parent = null) : base(fileMetaInfo.Title, parent)
    {
        IconPath = fileMetaInfo.IconPath;
        Properties = fileMetaInfo.Properties;
    }

    public FileNode(string filePath, BaseNode? parent = null) : base(Path.GetFileName(filePath), parent)
    {
        IconPath = filePath;
        Properties = new Property[]
        {
            new("Full path", filePath)
        };
    }
}