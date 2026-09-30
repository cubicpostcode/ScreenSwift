using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Forms = System.Windows.Forms;
using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;
using WpfPoint = System.Windows.Point;
using WpfBrushes = System.Windows.Media.Brushes;
using DrawingPixelFormat = System.Drawing.Imaging.PixelFormat;

namespace ScreenSwift;

internal sealed class SelectionWindow : Window
{
    private readonly Bitmap _source;
    private readonly Canvas _canvas = new();
    private readonly System.Windows.Shapes.Rectangle _shade = new() { Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(76, 0, 0, 0)) };
    private readonly System.Windows.Shapes.Rectangle _rectangle = new() { Stroke = WpfBrushes.DeepSkyBlue, StrokeThickness = 2, Visibility = Visibility.Collapsed };
    private readonly Polyline _line = new() { Stroke = WpfBrushes.Gold, StrokeThickness = 2, Visibility = Visibility.Collapsed };
    private readonly List<WpfPoint> _points = new();
    private WpfPoint _start;
    private bool _dragging;
    private readonly bool _freehand;
    private readonly bool _polygon;

    public event EventHandler<SelectionCapturedEventArgs>? SelectionCompleted;

    public SelectionWindow(Bitmap source, CaptureGesture gesture)
    {
        _source = source;
        _freehand = gesture == CaptureGesture.Freeform;
        _polygon = gesture == CaptureGesture.Polygon;
        var v = Forms.SystemInformation.VirtualScreen;
        Left = v.Left;
        Top = v.Top;
        Width = v.Width;
        Height = v.Height;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Topmost = true;
        Focusable = true;
        ShowInTaskbar = false;
        Background = WpfBrushes.Transparent;
        AllowsTransparency = true;

        _canvas.Children.Add(_shade);
        _canvas.Children.Add(_rectangle);
        _canvas.Children.Add(_line);
        Content = _canvas;

        Loaded += (_, _) =>
        {
            _shade.Width = ActualWidth;
            _shade.Height = ActualHeight;
            Activate();
            Keyboard.Focus(this);
        };
        MouseLeftButtonDown += OnLeftDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnLeftUp;
        PreviewKeyDown += OnKeyDown;
        KeyDown += OnKeyDown;
    }

    private void OnLeftDown(object sender, MouseButtonEventArgs e)
    {
        var point = e.GetPosition(_canvas);
        if (_polygon)
        {
            _line.Visibility = Visibility.Visible;
            if (_points.Count >= 3 && Distance(point, _points[0]) < 16)
            {
                CompletePath(_points);
                return;
            }
            _points.Add(point);
            UpdatePolyline(point);
            if (e.ClickCount == 2 && _points.Count >= 3) CompletePath(_points);
            e.Handled = true;
            return;
        }
        if (_polygon) return;

        _dragging = true;
        _start = point;
        _points.Clear();
        _points.Add(point);
        _rectangle.Visibility = _freehand ? Visibility.Collapsed : Visibility.Visible;
        _line.Visibility = _freehand ? Visibility.Visible : Visibility.Collapsed;
        CaptureMouse();
        e.Handled = true;
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        var point = e.GetPosition(_canvas);
        if (_polygon && _points.Count > 0) { UpdatePolyline(point); return; }
        if (!_dragging) return;
        if (_freehand)
        {
            _points.Add(point);
            UpdatePolyline(point);
        }
        else UpdateRectangle(point);
    }

    private void OnLeftUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging || _polygon) return;
        ReleaseMouseCapture();
        _dragging = false;
        var end = e.GetPosition(_canvas);
        if (_freehand)
        {
            _points.Add(end);
            if (_points.Count > 2) CompletePath(_points);
        }
        else
        {
            var area = NormalizedRectangle(_start, end);
            if (area.Width > 3 && area.Height > 3) CompleteRectangle(area);
        }
    }

    private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close();
            return;
        }
        if (e.Key == Key.Enter && _polygon && _points.Count >= 3) CompletePath(_points);
    }

    private void UpdateRectangle(WpfPoint point)
    {
        var r = NormalizedRectangle(_start, point);
        Canvas.SetLeft(_rectangle, r.X); Canvas.SetTop(_rectangle, r.Y);
        _rectangle.Width = r.Width; _rectangle.Height = r.Height;
    }

    private void UpdatePolyline(WpfPoint preview)
    {
        _line.Points.Clear();
        foreach (var point in _points) _line.Points.Add(point);
        if (_points.Count > 0) _line.Points.Add(preview);
        if (_polygon && _points.Count > 1) _line.Points.Add(_points[0]);
    }

    private void CompleteRectangle(Rect area)
    {
        var scaleX = _source.Width / ActualWidth;
        var scaleY = _source.Height / ActualHeight;
        var pixels = DrawingRectangle.FromLTRB(
            (int)Math.Floor(area.Left * scaleX), (int)Math.Floor(area.Top * scaleY),
            (int)Math.Ceiling(area.Right * scaleX), (int)Math.Ceiling(area.Bottom * scaleY));
        pixels.Intersect(new DrawingRectangle(0, 0, _source.Width, _source.Height));
        if (pixels.Width > 0 && pixels.Height > 0)
        {
            using var section = _source.Clone(pixels, DrawingPixelFormat.Format32bppPArgb);
            SelectionCompleted?.Invoke(this, new SelectionCapturedEventArgs((Bitmap)section.Clone(), isTransparent: false));
        }
        Close();
    }

    private void CompletePath(IReadOnlyList<WpfPoint> path)
    {
        var scaleX = _source.Width / ActualWidth;
        var scaleY = _source.Height / ActualHeight;
        var pixels = path.Select(p => new DrawingPoint((int)(p.X * scaleX), (int)(p.Y * scaleY))).ToArray();
        var left = pixels.Min(p => p.X); var top = pixels.Min(p => p.Y);
        var right = pixels.Max(p => p.X); var bottom = pixels.Max(p => p.Y);
        var bounds = DrawingRectangle.FromLTRB(left, top, right + 1, bottom + 1);
        bounds.Intersect(new DrawingRectangle(0, 0, _source.Width, _source.Height));
        if (bounds.Width > 0 && bounds.Height > 0)
        {
            var local = pixels.Select(p => new DrawingPoint(p.X - bounds.Left, p.Y - bounds.Top)).ToArray();
            var result = new Bitmap(bounds.Width, bounds.Height, DrawingPixelFormat.Format32bppPArgb);
            using var g = Graphics.FromImage(result);
            g.Clear(System.Drawing.Color.Transparent);
            using var region = new GraphicsPath();
            region.AddPolygon(local);
            g.SetClip(region);
            g.DrawImage(_source, new DrawingRectangle(0, 0, bounds.Width, bounds.Height), bounds, GraphicsUnit.Pixel);
            SelectionCompleted?.Invoke(this, new SelectionCapturedEventArgs(result, isTransparent: true));
        }
        Close();
    }

    private static Rect NormalizedRectangle(WpfPoint a, WpfPoint b) =>
        new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
    private static double Distance(WpfPoint a, WpfPoint b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
}

internal sealed class SelectionCapturedEventArgs : EventArgs
{
    public SelectionCapturedEventArgs(Bitmap image, bool isTransparent)
    {
        Image = image;
        IsTransparent = isTransparent;
    }

    public Bitmap Image { get; }
    public bool IsTransparent { get; }
}
