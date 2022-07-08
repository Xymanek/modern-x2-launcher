using ReactiveUI;

namespace ModernX2Launcher.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public string Greeting => "Welcome to Avalonia!";

        private ModListViewModel _modList = new();

        public MainWindowViewModel()
        {
            DesignTimeModListViewModel.PopulateDummy(_modList);
        }

        public ModListViewModel ModList
        {
            get => _modList;
            set => this.RaiseAndSetIfChanged(ref _modList, value);
        }
    }
}