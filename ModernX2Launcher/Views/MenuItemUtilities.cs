using System;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace ModernX2Launcher.Views;

public static class MenuItemUtilities
{
    public static MenuItem SetCommandFixedCanExecute(this MenuItem menuItem, ICommand command)
    {
        AttachCanExecuteFix(menuItem, command);

        return menuItem;
    }

    public static IDisposable AttachCanExecuteFix(this MenuItem menuItem, ICommand command)
    {
        return new CanExecuteFix(menuItem, command);
    }

    // This is as stupid as it looks. However, since MenuItem doesn't listen to CanExecute
    // changes while not attached to the logical tree (and the selection changes happen before
    // the menu is opened), our updates are lost. Additionally, both triggering the CanExecuteChanged
    // event and the MenuItem::CanExecuteChanged handler are not public, so the only way to force it
    // to run is via command (and command parameter) change handlers.
    // This will be fixed in avalonia v0.11
    private sealed class CanExecuteFix : IDisposable
    {
        private readonly MenuItem _menuItem;
        private readonly ICommand _command;

        public CanExecuteFix(MenuItem menuItem, ICommand command)
        {
            _menuItem = menuItem;
            _command = command;

            menuItem.AttachedToLogicalTree += OnAttachedToLogicalTree;
            menuItem.DetachedFromLogicalTree += OnDetachedFromLogicalTree;
        }

        private void OnAttachedToLogicalTree(object? o, LogicalTreeAttachmentEventArgs logicalTreeAttachmentEventArgs)
            => _menuItem.Command = _command;

        private void OnDetachedFromLogicalTree(object? o, LogicalTreeAttachmentEventArgs logicalTreeAttachmentEventArgs)
            => _menuItem.Command = null;

        public void Dispose()
        {
            _menuItem.AttachedToLogicalTree -= OnAttachedToLogicalTree;
            _menuItem.DetachedFromLogicalTree -= OnDetachedFromLogicalTree;
        }
    }
}