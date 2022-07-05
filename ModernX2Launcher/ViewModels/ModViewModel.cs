using ReactiveUI;

namespace ModernX2Launcher.ViewModels;

public class ModViewModel : ViewModelBase
{
    private bool _isEnabled;
    private string _title = "";

    public bool IsEnabled
    {
        get => _isEnabled;
        set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
    }

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }
}