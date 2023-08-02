using ArtHoarderArchive;

if (args.Length < 1)
{
    Console.WriteLine("Enter the command");
    Console.WriteLine(">");

    var s = Console.ReadLine();
    if (s != null) args = s.Split(' ');
}



var communicator = new NamedPipeCommunicator();
await communicator.SendCommandAsync(CommandCreator.Create(args));
