using System.IO.Pipes;

namespace ArtHoarderArchive;

public static class NamedPipeCommunicator
{
    public static Task SendCommandAsync(string text) => SendCommandAsync(text, new CancellationToken(false));

    public static async Task SendCommandAsync(string text, CancellationToken cancellationToken)
    {
        await using var pipeClient = new NamedPipeClientStream(".", "ArtHoarderArchive_Tasks", PipeDirection.InOut,
            PipeOptions.Asynchronous);
        try
        {
            await pipeClient.ConnectAsync(cancellationToken);
            var streamString = new StreamString(pipeClient);
            if (InitConnection(streamString))
            {
                streamString.WriteString(text);
                Listen(streamString);
            }

            pipeClient.Close();
        }
        catch (Exception e)
        {
            //TODO
            Console.WriteLine(e);
        }
    }

    private static bool InitConnection(StreamString streamString)
    {
        if (streamString.ReadString() == "OK")
            return true;

        Console.WriteLine("Service not OK");
        return false;
    }

    private static void Listen(StreamString streamString)
    {
        var commandExecutor = new CommandExecutor(streamString);
        var s = streamString.ReadString();
        while (s != null)
        {
            if (s.Length > 0 && s[0] == '#')
            {
                if (s == "#END")
                    return;

                commandExecutor.ExecuteCommand(s);
            }
            else
            {
                Printer.WriteMessage(s);
            }

            s = streamString.ReadString();
        }
    }
}
