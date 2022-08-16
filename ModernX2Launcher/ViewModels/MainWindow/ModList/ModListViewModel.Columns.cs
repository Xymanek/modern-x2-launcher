using System.Reactive;
using ReactiveUI;

namespace ModernX2Launcher.ViewModels.MainWindow.ModList;

public partial class ModListViewModel
{
    public class ColumnVisibility : ReactiveObject
    {
        private bool _isVisible = true;

        public ColumnVisibility()
        {
            Toggle = ReactiveCommand.Create(ToggleImpl);
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => this.RaiseAndSetIfChanged(ref _isVisible, value);
        }

        public ReactiveCommand<Unit, Unit> Toggle { get; }

        private void ToggleImpl()
        {
            IsVisible = !IsVisible;
        }
    }

    public ColumnVisibility ColumnVisibilityAuthor { get; } = new();
}