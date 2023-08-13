using System.Diagnostics.CodeAnalysis;
using ArgsParser.Attributes;
using ArtHoarderArchiveService.Archive;

namespace ArtHoarderArchiveService.PipeCommunications.Verbs;

[Verb("add", HelpText = "Add local user name or gallery profile to archive")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class AddVerb : BaseVerb
{
    [Group("target", true)]
    [Option('u', "user", 1, HelpText = "Add user")]
    public List<string>? UserNames { get; set; }

    [Group("target", true)]
    [Option('g', "gallery", 1, HelpText = "Add gallery")]
    public List<string>? Gallery { get; set; }

    [Option('a', "auto-names", HelpText = "Automatically assign names (if you add galleries)")]
    public bool AutoNames { get; set; }

    [Option('d', "download", HelpText = "TODO")] //TODO help text
    public bool Download { get; set; }


    public override bool Validate(out List<string>? errors)
    {
        errors = new List<string>();
        if (Gallery == null)
        {
            if (UserNames != null) return true;
            //TODO
            errors.Add("");
            return false;
        }

        if (AutoNames)
        {
            if (Gallery.All(link => Uri.TryCreate(link, UriKind.Absolute, out _)))
                return true;

            //TODO
            errors.Add("");
            return false;
        }

        if (Gallery.Count % 2 != 0)
        {
            //TODO
            errors.Add("");
            return false;
        }

        for (var i = 1; i < Gallery.Count; i += 2)
        {
            if (!Uri.TryCreate(Gallery[i], UriKind.Absolute, out _)) continue;

            //TODO
            errors.Add("");
            return false;
        }

        return true;
    }

    public override void Invoke(IMessager messager, ArchiveContextFactory archiveContextFactory, string path,
        CancellationToken cancellationToken)
    {
        var context = archiveContextFactory.CreateArchiveContext(messager, path, this);
        try
        {
            if (UserNames != null)
                AddUsers(messager, context);
            else
                AddGalleries(messager, context);
        }
        finally
        {
            archiveContextFactory.RealiseContext(path, this);
        }
    }

    private void AddUsers(IMessager statusWriter, ArchiveContext archiveContext)
    {
        if (UserNames == null)
            throw new Exception();

        archiveContext.TryAddNewUsers(statusWriter, UserNames);
    }

    private void AddGalleries(IMessager statusWriter, ArchiveContext archiveContext)
    {
        if (!AutoNames)
            AddGalleriesNoAutoNames(statusWriter, archiveContext);
        else
            AddGalleriesAutoNames(statusWriter, archiveContext);
    }

    private void AddGalleriesNoAutoNames(IMessager statusWriter, ArchiveContext archiveContext)
    {
        if (Gallery == null)
            throw new Exception();

        var uris = new List<Uri>();

        if (Gallery.Count % 2 != 0)
        {
            //TODO
            statusWriter.WriteLine("Args error");
            return;
        }

        for (var i = 1; i < Gallery.Count; i += 2)
        {
            if (Uri.TryCreate(Gallery[i], UriKind.Absolute, out var uri))
            {
                uris.Add(uri);
            }
            else
            {
                statusWriter.WriteLine($"\"{Gallery[i]}\" is not uri.");
            }
        }

        var names = new List<string>(uris.Count);
        for (var i = 0; i < Gallery.Count; i += 2)
        {
            names.Add(Gallery[i]);
        }

        archiveContext.TryAddNewGalleries(statusWriter, uris, names); //TODO
    }

    private void AddGalleriesAutoNames(IMessager statusWriter, ArchiveContext archiveContext)
    {
        if (Gallery == null)
            throw new Exception();

        var uris = new List<Uri>();
        foreach (var s in Gallery)
        {
            if (Uri.TryCreate(s, UriKind.Absolute, out var uri))
            {
                uris.Add(uri);
            }
            else
            {
                statusWriter.WriteLine($"\"{s}\" is not uri.");
            }
        }

        archiveContext.TryAddNewGalleries(statusWriter, uris); //TODO
    }
}
