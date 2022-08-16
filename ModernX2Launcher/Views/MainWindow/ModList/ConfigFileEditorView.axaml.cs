using System;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;

namespace ModernX2Launcher.Views.MainWindow.ModList;

public partial class ConfigFileEditorView : UserControl
{
    public ConfigFileEditorView()
    {
        InitializeComponent();

        Editor.Background = Brushes.Transparent;

        RegistryOptions registryOptions = new(ThemeName.Dark);
        TextMate.Installation textMate = Editor.InstallTextMate(registryOptions);

        textMate.SetGrammar(registryOptions.GetScopeByExtension(".ini"));

        Editor.Document = new TextDocument("[X2Things]" + Environment.NewLine + "+arrStuff=123");
    }
}