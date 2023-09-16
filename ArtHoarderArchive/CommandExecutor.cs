using System.Text.Json;
using System.Text.RegularExpressions;

namespace ArtHoarderArchive;

public class CommandExecutor
{
    private const string MsgCommand = "#Msg ";
    private const string PrintFileCommand = "#PrintFile ";
    private const string UpdatePbCommand = "#Update ";
    private const string LogCommand = "#Log ";
    private const string ReadLineCommand = "#ReadLine";

    private readonly StreamString _streamString;

    public CommandExecutor(StreamString streamString)
    {
        _streamString = streamString;
    }

    public void ExecuteCommand(string command)
    {
        if (command.StartsWith(MsgCommand))
        {
            ParsMsg(command[MsgCommand.Length..]);
        }
        else if (command.StartsWith(LogCommand))
        {
            ParsLog(command[LogCommand.Length..]);
        }
        else if (command.StartsWith(UpdatePbCommand))
        {
            ParsUpdateProgress(command[UpdatePbCommand.Length..]);
        }
        else if (command.StartsWith(PrintFileCommand))
        {
            foreach (var line in File.ReadLines(command[PrintFileCommand.Length..]))
            {
                Console.WriteLine(line);
            }
        }
        else if (command == ReadLineCommand)
        {
            var line = Console.ReadLine();
            if (line != null)
                _streamString.WriteString(line);
        }
        else if (command.StartsWith("#"))
        {
        }
    }

    private static void ParsMsg(string command)
    {
        if (Enum.TryParse(command[..command.IndexOf(' ')], out MessageType msgType))
        {
            Printer.WriteMessage(msgType, command[(command.IndexOf(' ') + 1)..]);
        }
        else
        {
            Printer.WriteMessage(command);
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
