using System.Runtime.InteropServices;
using System.Windows.Controls;
using D = System.Drawing;
using F = System.Windows.Forms;
using I = System.Windows.Input;
using W = System.Windows;
using WMI = System.Windows.Media.Imaging;

namespace deepseek_copilot;

public partial class ScreenCaptureWindow : W.Window
{
    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    public static void ForceForeground(IntPtr hWnd)
    {
        var fgThread = GetWindowThreadProcessId(GetForegroundWindow(), out _);
        var currentThread = GetCurrentThreadId();
        if (fgThread != 0 && fgThread != currentThread)
        {
            AttachThreadInput(currentThread, fgThread, true);
            BringWindowToTop(hWnd);
            SetForegroundWindow(hWnd);
            AttachThreadInput(currentThread, fgThread, false);
        }
        else
        {
            BringWindowToTop(hWnd);
            SetForegroundWindow(hWnd);
        }
    }

    private WMI.BitmapSource? _screenSource;
    private W.Point _start;
    private W.Rect _selection;
    private bool _drawing;
    private readonly double _scaleX;
    private readonly double _scaleY;

    public event Action<WMI.BitmapSource>? Confirmed;
    public event Action? Cancelled;

    public ScreenCaptureWindow()
    {
        InitializeComponent();

        var vs = F.SystemInformation.VirtualScreen;
        Left = W.SystemParameters.VirtualScreenLeft;
        Top = W.SystemParameters.VirtualScreenTop;
        Width = W.SystemParameters.VirtualScreenWidth;
        Height = W.SystemParameters.VirtualScreenHeight;

        _scaleX = vs.Width / W.SystemParameters.VirtualScreenWidth;
        _scaleY = vs.Height / W.SystemParameters.VirtualScreenHeight;

        Loaded += OnLoaded;
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void OnLoaded(object sender, W.RoutedEventArgs e)
    {
        CaptureScreen();
        Activate();
        I.Keyboard.Focus(this);
        ForceForeground(new W.Interop.WindowInteropHelper(this).Handle);
    }

    private void CaptureScreen()
    {
        var vs = F.SystemInformation.VirtualScreen;
        using var bitmap = new D.Bitmap(vs.Width, vs.Height, D.Imaging.PixelFormat.Format32bppArgb);
        using (var g = D.Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = D.Drawing2D.SmoothingMode.HighQuality;
            g.CopyFromScreen(vs.Left, vs.Top, 0, 0, new D.Size(vs.Width, vs.Height),
                D.CopyPixelOperation.SourceCopy);
        }

        var hBitmap = bitmap.GetHbitmap();
        try
        {
            _screenSource = W.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero,
                W.Int32Rect.Empty, WMI.BitmapSizeOptions.FromEmptyOptions());
        }
        finally
        {
            DeleteObject(hBitmap);
        }

        ScreenImage.Width = Width;
        ScreenImage.Height = Height;
        ScreenImage.Source = _screenSource;
    }

    private void OnMouseLeftButtonDown(object sender, I.MouseButtonEventArgs e)
    {
        _start = e.GetPosition(RootCanvas);
        _selection = new W.Rect(_start, new W.Size(0, 0));
        _drawing = true;
        HintText.Visibility = W.Visibility.Collapsed;
        ActionPanel.Visibility = W.Visibility.Collapsed;
        SelectionBorder.Visibility = W.Visibility.Collapsed;
        CaptureMouse();
    }

    private void OnMouseMove(object sender, I.MouseEventArgs e)
    {
        if (!_drawing) return;

        var current = e.GetPosition(RootCanvas);
        _selection = new W.Rect(_start, current);
        UpdateSelectionVisual();
    }

    private void OnMouseLeftButtonUp(object sender, I.MouseButtonEventArgs e)
    {
        if (!_drawing) return;

        _drawing = false;
        ReleaseMouseCapture();

        if (_selection.Width < 3 || _selection.Height < 3)
        {
            ResetSelection();
            return;
        }

        ShowActionPanel();
    }

    private void UpdateSelectionVisual()
    {
        var r = _selection;
        var canvasWidth = RootCanvas.ActualWidth;
        var canvasHeight = RootCanvas.ActualHeight;

        DarkLeft.Width = r.X;
        DarkLeft.Height = canvasHeight;
        Canvas.SetLeft(DarkLeft, 0);
        Canvas.SetTop(DarkLeft, 0);

        DarkTop.Width = r.Width;
        DarkTop.Height = r.Y;
        Canvas.SetLeft(DarkTop, r.X);
        Canvas.SetTop(DarkTop, 0);

        DarkRight.Width = canvasWidth - (r.X + r.Width);
        DarkRight.Height = canvasHeight;
        Canvas.SetLeft(DarkRight, r.X + r.Width);
        Canvas.SetTop(DarkRight, 0);

        DarkBottom.Width = r.Width;
        DarkBottom.Height = canvasHeight - (r.Y + r.Height);
        Canvas.SetLeft(DarkBottom, r.X);
        Canvas.SetTop(DarkBottom, r.Y + r.Height);

        SelectionBorder.Width = r.Width;
        SelectionBorder.Height = r.Height;
        Canvas.SetLeft(SelectionBorder, r.X);
        Canvas.SetTop(SelectionBorder, r.Y);
        SelectionBorder.Visibility = W.Visibility.Visible;
    }

    private void ResetSelection()
    {
        SelectionBorder.Visibility = W.Visibility.Collapsed;
        ActionPanel.Visibility = W.Visibility.Collapsed;
        DarkLeft.Width = 0;
        DarkTop.Width = 0;
        DarkRight.Width = 0;
        DarkBottom.Width = 0;
    }

    private void ShowActionPanel()
    {
        ActionPanel.Visibility = W.Visibility.Visible;
        ActionPanel.Measure(new W.Size(double.PositiveInfinity, double.PositiveInfinity));
        ActionPanel.UpdateLayout();

        var r = _selection;
        var panelWidth = ActionPanel.ActualWidth;
        var panelHeight = ActionPanel.ActualHeight;

        var x = r.X + r.Width - panelWidth;
        var y = r.Y + r.Height + 6;
        if (y + panelHeight > RootCanvas.ActualHeight)
            y = r.Y - panelHeight - 6;
        x = Math.Max(0, Math.Min(x, RootCanvas.ActualWidth - panelWidth));
        y = Math.Max(0, y);

        Canvas.SetLeft(ActionPanel, x);
        Canvas.SetTop(ActionPanel, y);
    }

    private void OnConfirmClick(object sender, W.RoutedEventArgs e)
    {
        var image = CropSelection();
        if (image == null)
        {
            Close();
            return;
        }

        W.Clipboard.SetImage(image);
        Confirmed?.Invoke(image);
        Close();
    }

    private WMI.BitmapSource? CropSelection()
    {
        if (_screenSource == null) return null;

        var x = (int)Math.Round(_selection.X * _scaleX);
        var y = (int)Math.Round(_selection.Y * _scaleY);
        var w = (int)Math.Round(_selection.Width * _scaleX);
        var h = (int)Math.Round(_selection.Height * _scaleY);

        return new WMI.CroppedBitmap(_screenSource, new W.Int32Rect(x, y, Math.Max(1, w), Math.Max(1, h)));
    }

    private void OnCancelClick(object sender, W.RoutedEventArgs e)
    {
        Cancelled?.Invoke();
        Close();
    }

    private void OnPreviewKeyDown(object sender, I.KeyEventArgs e)
    {
        if (e.Key == I.Key.Escape)
        {
            Cancelled?.Invoke();
            Close();
        }
        else if (e.Key == I.Key.Enter && ActionPanel.Visibility == W.Visibility.Visible)
        {
            OnConfirmClick(sender, e);
        }
    }
}