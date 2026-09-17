using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Rumours.App;

public partial class OverlayWindow : Window
{
    public event Action<Point>? Moved;

    public OverlayWindow(OverlayViewModel viewModel, Settings settings)
    {
        InitializeComponent();
        DataContext = viewModel;
        (Left, Top) = ScreenPlacement.Visible(new Point(settings.OverlayLeft, settings.OverlayTop));
    }

    public bool HiddenFromCapture { get; private set; }

    public HwndSource Source => (HwndSource)PresentationSource.FromVisual(this);

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var handle = new WindowInteropHelper(this).Handle;

        var style = Native.GetWindowLongPtr(handle, Native.GwlExStyle).ToInt64();
        style |= Native.WsExToolWindow | Native.WsExNoActivate;
        Native.SetWindowLongPtr(handle, Native.GwlExStyle, new IntPtr(style));
        SetClickThrough(true);

        HiddenFromCapture = Native.SetWindowDisplayAffinity(handle, Native.WdaExcludeFromCapture);
    }

    public void SetMovable(bool movable, string hint)
    {
        MoveCue.Visibility = movable ? Visibility.Visible : Visibility.Collapsed;
        MoveCueText.Text = hint;
        SetClickThrough(!movable);
        if (movable) Show();
    }

    private void SetClickThrough(bool clickThrough)
    {
        var handle = new WindowInteropHelper(this).Handle;
        var style = Native.GetWindowLongPtr(handle, Native.GwlExStyle).ToInt64();
        style = clickThrough ? style | Native.WsExTransparent : style & ~(long)Native.WsExTransparent;
        Native.SetWindowLongPtr(handle, Native.GwlExStyle, new IntPtr(style));
    }

    private void DragStarted(object sender, MouseButtonEventArgs e)
    {
        this.DragSafely();
        Moved?.Invoke(new Point(Left, Top));
    }
}
