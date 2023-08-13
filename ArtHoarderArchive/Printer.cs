namespace ArtHoarderArchive;

public static class Printer
{
    private const string Cross = " ├─";
    private const string Corner = " └─";
    private const string Vertical = " │ ";
    private const string Space = "   ";
    private const char LoadingBarSpace = ' ';
    private const int LoadingSegments = 10;

    private static readonly char[] ProgressChars =
        { '░', '▒', '▓', '█' };

    private static readonly int Positions = ProgressChars.Length * LoadingSegments;

    private static int _progressBarsOffset = 0;
    private static ProgressBar? _progressBar = null;

    public static void WriteMessage(string message)
    {
        var pos = Console.CursorTop - _progressBarsOffset;
        if (pos < 0) pos = 0;
        Console.SetCursorPosition(0, pos);
        Console.WriteLine(message);
        PrintAddBars();
    }

    public static void UpdateBar(ProgressBar progressBar)
    {
        _progressBar = progressBar;
        var pos = Console.CursorTop - _progressBarsOffset;
        if (pos < 0) pos = 0;
        Console.SetCursorPosition(0, pos);
        PrintAddBars();
    }

    public static void ClearProgress()
    {
        _progressBar = null;
        _progressBarsOffset = 0;
    }

    private static void PrintProgressBarInfo(ProgressBar progressBar) //TODO clear end of line
    {
        var value = (double)progressBar.Current / progressBar.Max;
        try
        {
            value = value > 1 ? 1d : Math.Round(value, 1);
        }
        catch
        {
            value = 1d;
        }

        value *= LoadingSegments;

        var spaceCount = (int)Math.Floor(value);
        var loadingBar = new string(ProgressChars[^1], spaceCount);
        spaceCount = LoadingSegments - spaceCount - 1;
        value = (value % 1) * ProgressChars.Length;
        loadingBar += ProgressChars[(int)Math.Floor(value)];
        if (spaceCount > 0)
            loadingBar += new string(LoadingBarSpace, spaceCount);

        Console.Write($"{progressBar.Name} [{loadingBar}] {progressBar.Msg}");
    }


    private static void PrintAddBars()
    {
        if (_progressBar == null) return;
        _progressBarsOffset = 0;
        PrintProgressBar(_progressBar, "");

        void PrintProgressBar(ProgressBar progressBar, string indent)
        {
            PrintProgressBarInfo(progressBar);
            Console.WriteLine();
            _progressBarsOffset++;

            for (var i = 0; i < progressBar.SubBars.Count; i++)
            {
                var subBar = progressBar.SubBars[i];
                PrintSubProgressBar(subBar, indent, i == progressBar.SubBars.Count - 1);
            }
        }

        void PrintSubProgressBar(ProgressBar progressBar, string indent, bool isLast)
        {
            Console.Write(indent);

            if (isLast)
            {
                Console.Write(Corner);
                indent += Space;
            }
            else
            {
                Console.Write(Cross);
                indent += Vertical;
            }

            PrintProgressBar(progressBar, indent);
        }
    }
}
