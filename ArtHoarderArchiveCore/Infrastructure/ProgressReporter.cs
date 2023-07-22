namespace ArtHoarderCore.Infrastructure;

public class ProgressReporter
{
    private readonly Action<string> _stageSetter;
    private readonly Action<double> _progressValueSetter;
    private readonly Action<double> _progressMaxValueSetter;
    private readonly Action<string> _reporter;
    private readonly Action<SubProgressInfo> _addSubProgress;
    private readonly Action<SubProgressInfo> _removeSubProgress;
    private readonly Action<string> _showError;

    private double _currentProgressValue;
    private double _maxProgressValue;

    public bool IsError;

    public ProgressReporter(Action<string> stageSetter, Action<double> progressValueSetter,
        Action<double> progressMaxValueSetter,
        Action<string> reporter, Action<SubProgressInfo> addSubProgress, Action<SubProgressInfo> removeSubProgress,
        Action<string> showError)
    {
        _stageSetter = stageSetter;
        _progressValueSetter = progressValueSetter;
        _progressMaxValueSetter = progressMaxValueSetter;
        _reporter = reporter;
        _addSubProgress = addSubProgress;
        _removeSubProgress = removeSubProgress;
        _showError = showError;

        SetProgressStage("Progress...");
        SetProgressBar(100, 100);
    }

    public SubProgressInfo CreateSubProgress(string title, double totalValue)
    {
        var progress = new SubProgressInfo(title, totalValue, _reporter, _removeSubProgress);
        _addSubProgress(progress);
        return progress;
    }

    public void SetProgressStage(string stage)
    {
        _stageSetter(stage);
    }

    public void Progress()
    {
        _progressValueSetter(++_currentProgressValue);
    }

    public void ReportAndProgress(string message)
    {
        _reporter(message);
        _progressValueSetter(++_currentProgressValue);
    }

    public void Report(string message)
    {
        _reporter(message);
    }

    public void Error(string message)
    {
        IsError = true;
        _showError(message);
    }

    public void SetProgressBar(double current, double max)
    {
        _currentProgressValue = current;
        _maxProgressValue = max;

        _progressValueSetter(_currentProgressValue);
        _progressMaxValueSetter(_maxProgressValue);
    }
}