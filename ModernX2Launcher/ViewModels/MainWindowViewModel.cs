using ReactiveUI;

namespace ModernX2Launcher.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private ModListViewModel _modList = new();
        private ModInfoViewModel _modInfo = new();

        public MainWindowViewModel()
        {
            DesignTimeModListViewModel.PopulateDummy(_modList);
        }

        public ModListViewModel ModList
        {
            get => _modList;
            set => this.RaiseAndSetIfChanged(ref _modList, value);
        }

        public ModInfoViewModel ModInfo
        {
            get => _modInfo;
            set => this.RaiseAndSetIfChanged(ref _modInfo, value);
        }
    }
}