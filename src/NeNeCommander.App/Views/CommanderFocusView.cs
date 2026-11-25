using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.App.Views;

/// <summary>Adapts native focus classification and row focus without owning session or command work.</summary>
internal static class CommanderFocusView
{
    internal static KeyboardContext CurrentContext(FrameworkElement root, CommandEntryView commands, PaneFilterView filters)
    {
        object? focused = FocusManager.GetFocusedElement(root.XamlRoot);
        return filters.ContainsInput(focused) ? KeyboardContext.PaneFilter
            : focused is TextBox or RichEditBox or PasswordBox or AutoSuggestBox
                ? KeyboardContext.TextEntry
                : commands.ContainsFocus(focused) || focused is Button
                    ? KeyboardContext.CommandEntry : KeyboardContext.FileList;
    }

    internal static void FocusFileList(ListView fileList)
    {
        Control target = fileList.SelectedItem is object selected && fileList.ContainerFromItem(selected) is ListViewItem row
            ? row
            : fileList;
        if (!target.Focus(FocusState.Programmatic))
        {
            _ = fileList.Focus(FocusState.Programmatic);
        }
    }

}
