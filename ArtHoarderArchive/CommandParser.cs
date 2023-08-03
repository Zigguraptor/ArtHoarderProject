﻿using System.Text.Json;
using System.Text.RegularExpressions;

namespace ArtHoarderArchive;

public static class CommandParser
{
    public static void ParsCommand(string command)
    {
        if (command.StartsWith("#Log "))
        {
            ParsLog(command[5..]);
        }
        else if (command.StartsWith("#Update "))
        {
            ParsUpdateProgress(command[8..]);
        }
    }

    private static void ParsUpdateProgress(string command)
    {
        var progressBar = JsonSerializer.Deserialize<ProgressBar>(command);
        if (progressBar == null) return;
        
        Printer.UpdateBar(progressBar);
    }

    private static void ParsLog(string command)
    {
        var strings = SplitStrings(command);
        if (strings.Length != 2) return; //TODO log

        Printer.WriteMessage($"[{strings[0]}] {strings[1]}");
    }

    private static string[] SplitStrings(string s)
    {
        if (s.Length > 1)
        {
            s = s[1..];
            s = s[..^1];
        }

        var args = Regex.Split(s, "(?<=\\\\)*\" \"");
        for (var i = 0; i < args.Length; i++)
            args[i] = Unescape(args[i]);
        return args;
    }

    private static string Unescape(string s)
    {
        s = s.Replace("\\\\", "\\");
        return s.Replace("\\\"", "\"");
    }
}
