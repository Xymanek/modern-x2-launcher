using ReactiveUI;

namespace ModernX2Launcher.ViewModels;

public class ViewModelBase : ReactiveObject, IActivatableViewModel
{
    ViewModelActivator IActivatableViewModel.Activator { get; } = new();
}