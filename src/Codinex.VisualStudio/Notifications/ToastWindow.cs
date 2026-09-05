using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Codinex.VisualStudio.Notifications;

/// <summary>
/// A borderless, non-activating popup shown near the system tray clock. Built entirely in
/// code (no XAML), so it can be restyled by editing this constructor directly - no designer
/// or XAML build step involved. Never call <see cref="Window.Activate"/> on it: it must not
/// steal focus from whatever the user is doing.
/// </summary>
public sealed class ToastWindow : Window
{
    /// <summary>Raised when the user clicks the toast body (not the close button).</summary>
    public event EventHandler ToastClicked;

    public ToastWindow(string title, string message)
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        Topmost = true;
        ShowActivated = false;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.Height;
        Width = 320;

        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x26)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x3f, 0x3f, 0x46)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(14, 10, 14, 12),
            Cursor = Cursors.Hand
        };

        var headerRow = new DockPanel();

        var closeButton = new Button
        {
            Content = "✕",
            Width = 20,
            Height = 20,
            Padding = new Thickness(0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Foreground = new SolidColorBrush(Color.FromRgb(0xa0, 0xa0, 0xa0)),
            Cursor = Cursors.Hand,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        DockPanel.SetDock(closeButton, Dock.Right);
        closeButton.Click += (_, _) => Close();
        headerRow.Children.Add(closeButton);

        var titleBlock = new TextBlock
        {
            Text = title,
            FontWeight = FontWeights.Bold,
            FontSize = 13,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };
        headerRow.Children.Add(titleBlock);

        var messageBlock = new TextBlock
        {
            Text = message,
            Foreground = new SolidColorBrush(Color.FromRgb(0xcc, 0xcc, 0xcc)),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 0)
        };

        var stack = new StackPanel();
        stack.Children.Add(headerRow);
        stack.Children.Add(messageBlock);

        border.Child = stack;
        border.MouseLeftButtonUp += (_, _) => ToastClicked?.Invoke(this, EventArgs.Empty);

        Content = border;
    }
}
