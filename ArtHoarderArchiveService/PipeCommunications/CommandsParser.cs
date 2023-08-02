using System.Text.RegularExpressions;
using ArgsParser;
using ArtHoarderArchiveService.PipeCommunications.Verbs;

namespace ArtHoarderArchiveService.PipeCommunications;

public class CommandsParser : ICommandsParser
{
    private readonly ArgsParser.ArgsParser _argsParser;

    public CommandsParser()
    {
        _argsParser = new ArgsParserBuilder()
            .AddVerb<InitVerb>()
            .AddVerb<LoginVerb>()
            .AddVerb<AddVerb>()
            .AddVerb<UpdateVerb>()
            .AddVerb<StatusVerb>()
            .Build();
    }

    public ArtHoarderTask ParsCommand(string command)
    {
        var strings = SplitCommand(command);
        var path = strings[0];
        var args = strings[2..];

        return new ArtHoarderTask(path, _argsParser.ParseArgs(args));
    }


    private static string[] SplitCommand(string s)
    {
        if (s.Length > 1)
        {
            s = s[1..];
            s = s[..^1];
        }

        var args = Regex.Split(s, "(?<=\\\\)*\" \"");
        return args;
    }
}
