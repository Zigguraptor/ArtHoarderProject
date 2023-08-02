using System.IO.Pipes;

namespace ArtHoarderArchive;

public class NamedPipeCommunicator
{
    public Task SendCommandAsync(string text)
    {
        return SendCommandAsync(text, new CancellationToken(false));
    }

    public async Task SendCommandAsync(string text, CancellationToken cancellationToken)
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

    private bool InitConnection(StreamString streamString)
    {
        if (streamString.ReadString() == "OK")
            return true;

        Console.WriteLine("Service not OK");
        return false;
    }

    private void Listen(StreamString streamString)
    {
        var s = streamString.ReadString();
        while (s != null)
        {
            if (s[0] == '#')
            {
                //TODO
            }
            else
            {
                Console.WriteLine(s);
            }

            s = streamString.ReadString();
        }
    }
}
