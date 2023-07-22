using System.IO;
using ArtHoarderCore.DAL.Entities;

namespace ArtHoarderClient.Infrastructure.FoldersAndItems;

public class FolderNode : BaseNode
{
    protected int FoldersCount => Children.Count;
    protected int ItemsCount => Children.Count;
    protected int TotalCount => Children.Count;
    public override string IconPath => Path.GetFullPath(MyConstants.StaticFolderIconLocalPath);
    public override bool IsContainer => true;

    public override Property[] Properties
    {
        get
        {
            var result = new Property[4];
            result[0] = new Property("Name", Title);
            result[1] = new Property("Folders", FoldersCount.ToString());
            result[2] = new Property("Items", ItemsCount.ToString());
            result[3] = new Property("Total", TotalCount.ToString());
            return result;
        }
    }

    public FolderNode(string title, BaseNode? parent = null) : base(title, parent)
    {
    }
}