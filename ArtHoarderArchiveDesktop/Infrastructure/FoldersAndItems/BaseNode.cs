using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ArtHoarderCore.DAL.Entities;
using ArtHoarderCore.Infrastructure;
using ArtHoarderCore.Managers;

namespace ArtHoarderClient.Infrastructure.FoldersAndItems;

public abstract class BaseNode : IDisplayViewItem
{
    public virtual string Title { get; }
    public abstract string IconPath { get; }
    public abstract bool IsContainer { get; }
    public List<BaseNode> Children { get; set; } = new();
    [JsonIgnore] public BaseNode? Parent { get; set; }
    [JsonIgnore] public virtual Property[] Properties => Array.Empty<Property>();

    public BaseNode(string title, BaseNode? parent = null)
    {
        Title = title;
        Parent = parent;
    }

    public virtual List<IDisplayViewItem> GetViewItems(Archive archive)
    {
        return new List<IDisplayViewItem>(Children);
    }

    protected virtual void Refresh(Archive archive)
    {
    }

    public List<BaseNode> GetBreadcrumbs()
    {
        var result = new List<BaseNode> { this };
        var current = this;
        while (current.Parent != null)
        {
            current = current.Parent;
            result.Add(current);
        }

        result.Reverse();
        return result;
    }
}