using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Rumours.App;

internal static class WindowDragging
{
    public static void DragSafely(this Window window)
    {
        if (Mouse.LeftButton == MouseButtonState.Pressed) window.DragMove();
    }
}

public class PoeWindow : Window
{
    public PoeWindow() => Style = (Style)Application.Current.FindResource("PoeWindow");

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (GetTemplateChild("PART_Close") is Button close) close.Click += (_, _) => Close();
        if (GetTemplateChild("PART_Header") is UIElement header)
            header.MouseLeftButtonDown += (_, _) => this.DragSafely();
    }
}
