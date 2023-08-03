﻿using System.Text.RegularExpressions;
using ArgsParser;
using ArtHoarderArchiveService.PipeCommunications.Verbs;

namespace ArtHoarderArchiveService.PipeCommunications;

public class CommandsParser : ICommandsParser
{
    private readonly ILogger<CommandsParser> _logger;
    private readonly ArgsParser.ArgsParser _argsParser;

    public CommandsParser(ILogger<CommandsParser> logger)
    {
        _logger = logger;
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

        path = Unescape(path);

        for (var i = 0; i < args.Length; i++)
        {
            args[i] = Unescape(args[i]);
        }

        var parsedObj = _argsParser.ParseArgs(args);

        if (parsedObj is BaseVerb verb)
        {
            return (path, verb);
        }

        _logger.LogError("Command parsing error. Parsed object is no BaseVerb.");
        throw new InvalidCastException("parsed object is no ");
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

    private static string Unescape(string s)
    {
        s = s.Replace("\\\\", "\\");
        return s.Replace("\\\"", "\"");
    }
}
