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

    public object ParsCommand(string command) => _argsParser.ParseArgs(command.Trim().Split(' '));
}
