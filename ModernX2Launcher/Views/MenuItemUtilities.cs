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