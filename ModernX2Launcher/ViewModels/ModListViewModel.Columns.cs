using System.Reactive;
using ReactiveUI;

namespace ModernX2Launcher.ViewModels;

public partial class ModListViewModel
{
    private bool _columnVisibleAuthor = true;

    public bool ColumnVisibleAuthor
    {
        get => _columnVisibleAuthor;
        set => this.RaiseAndSetIfChanged(ref _columnVisibleAuthor, value);
    }

    public ReactiveCommand<Unit, Unit> ToggleColumnVisibilityAuthor { get; }
}