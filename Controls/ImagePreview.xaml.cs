using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PixivTools.Controls;

public partial class ImagePreview : UserControl
{
    public static readonly DependencyProperty SourceProperty = 
        DependencyProperty.Register(nameof(Source), typeof(object), typeof(ImagePreview), 
            new PropertyMetadata(null, OnSourceChanged));
    public static readonly DependencyProperty PlaceholderTextProperty = 
        DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(ImagePreview), 
            new PropertyMetadata(""));
    public static readonly DependencyProperty IsLoadingProperty = 
        DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(ImagePreview), 
            new PropertyMetadata(false, OnIsLoadingChanged));

    public object? Source { get => GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    public string PlaceholderText { get => (string)GetValue(PlaceholderTextProperty); set => SetValue(PlaceholderTextProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value); }

    private Point _last; private bool _drag;

    public ImagePreview() => InitializeComponent();

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var c = (ImagePreview)d;
        c.Placeholder.Visibility = e.NewValue != null ? Visibility.Collapsed : Visibility.Visible;
        if (e.NewValue != null) c.LoadingBar.Visibility = Visibility.Collapsed;
    }

    private static void OnIsLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var c = (ImagePreview)d;
        var loading = (bool)e.NewValue;
        c.LoadingBar.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
        if (loading) c.Placeholder.Visibility = Visibility.Visible;
    }

    private void OnMouseWheel(object s, MouseWheelEventArgs e)
    {
        var f = e.Delta > 0 ? 1.1 : 0.9;
        ScaleTransform.ScaleX = Math.Clamp(ScaleTransform.ScaleX * f, 0.1, 10.0);
        ScaleTransform.ScaleY = Math.Clamp(ScaleTransform.ScaleY * f, 0.1, 10.0);
    }

    private void OnMouseLeftButtonDown(object s, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ScaleTransform.ScaleX = 1; ScaleTransform.ScaleY = 1;
            TranslateTransform.X = 0; TranslateTransform.Y = 0;
            return;
        }
        if (ScaleTransform.ScaleX > 1.0) { _drag = true; _last = e.GetPosition(this); MainImage.CaptureMouse(); }
    }

    private void OnMouseLeftButtonUp(object s, MouseButtonEventArgs e) { _drag = false; MainImage.ReleaseMouseCapture(); }
    private void OnMouseMove(object s, MouseEventArgs e)
    {
        if (_drag) { var p = e.GetPosition(this); var d = p - _last; TranslateTransform.X += d.X; TranslateTransform.Y += d.Y; _last = p; }
    }
}
