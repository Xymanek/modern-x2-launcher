using System;
using ModernX2Launcher.ViewModels.Common;

namespace ModernX2Launcher.ViewModels.MainWindow.ModList.Filtering;

public interface IModListFilter
{
    string DisplayLabel { get; }

    IObservable<bool> DoesModPass(ModEntryViewModel mod);
}
