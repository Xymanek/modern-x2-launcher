using ReactiveUI;

namespace ModernX2Launcher.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public string Greeting => "Welcome to Avalonia!";

        private ModListViewModel _modList = new();

        public ModListViewModel ModList
        {
            get => _modList;
            set => this.RaiseAndSetIfChanged(ref _modList, value);
        }
    }
}