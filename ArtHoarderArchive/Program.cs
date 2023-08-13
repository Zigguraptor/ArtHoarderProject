using ArtHoarderArchive;

if (args.Length < 1)
{
    await NoArgsMode();
    return;
}

await NamedPipeCommunicator.SendCommandAsync(CommandCreator.Create(args), new CancellationToken(false));


async Task NoArgsMode()
{
    Console.WriteLine("Enter the command");
    while (true)
    {
        Console.Write(">");

        var s = Console.ReadLine();
        if (string.IsNullOrEmpty(s)) break;
        args = s.Split(' ');
        await NamedPipeCommunicator.SendCommandAsync(CommandCreator.Create(args), new CancellationToken(false));
    }
}
