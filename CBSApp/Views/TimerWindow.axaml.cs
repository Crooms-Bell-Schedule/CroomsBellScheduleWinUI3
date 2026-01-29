using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace CBSApp.Views;

public partial class TimerWindow : Window
{
    private bool _mouseDownForWindowMoving = false;
    private PointerPoint _originalPoint;
    public TimerWindow()
    {
        InitializeComponent();
    }

    private void Window_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (!_mouseDownForWindowMoving) return;

        PointerPoint currentPoint = e.GetCurrentPoint(this);
        Position = new PixelPoint(Position.X + (int)(currentPoint.Position.X - _originalPoint.Position.X),
            Position.Y + (int)(currentPoint.Position.Y - _originalPoint.Position.Y));
    }

    private void Window_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (WindowState == WindowState.Maximized || WindowState == WindowState.FullScreen) return;

        _mouseDownForWindowMoving = true;
        _originalPoint = e.GetCurrentPoint(this);
    }

    private void Window_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        _mouseDownForWindowMoving = false;
    }
}
