using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;

namespace Descreen;

public partial class BreakOverlayWindow : Window
{
    private readonly TimerManager _timer;
    private readonly DispatcherTimer _uiTimer;
    private Arc? _progressArc;
    private double _totalDuration;

    public BreakOverlayWindow(TimerManager timer)
    {
        InitializeComponent();
        _timer = timer;
        _totalDuration = timer.BreakDuration;

        MessageLabel.Text = timer.BreakMessage;

        BuildRing();

        _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _uiTimer.Tick += Tick;
        _uiTimer.Start();

        // Close overlay when break ends
        timer.OnBreakEnd = () => Dispatcher.UIThread.Post(() =>
        {
            _uiTimer.Stop();
            Close();
        });
    }

    private void BuildRing()
    {
        const double cx = 110, cy = 110, r = 90, stroke = 12;

        // Track circle
        var track = new Ellipse
        {
            Width = r * 2, Height = r * 2,
            Stroke = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
            StrokeThickness = stroke
        };
        Canvas.SetLeft(track, cx - r);
        Canvas.SetTop(track,  cy - r);
        RingCanvas.Children.Add(track);

        // Progress arc (Avalonia Arc: StartAngle=-90, SweepAngle=360 = full circle)
        _progressArc = new Arc
        {
            Width = r * 2, Height = r * 2,
            Stroke = new SolidColorBrush(Colors.Cyan),
            StrokeThickness = stroke,
            StartAngle = -90,
            SweepAngle = 360,
            StrokeLineCap = PenLineCap.Round
        };
        Canvas.SetLeft(_progressArc, cx - r);
        Canvas.SetTop(_progressArc,  cy - r);
        RingCanvas.Children.Add(_progressArc);
    }

    private void Tick(object? sender, EventArgs e)
    {
        double remaining = _timer.GetRemainingBreakTime();
        CountdownLabel.Text = ((int)Math.Ceiling(remaining)).ToString();

        if (_progressArc != null && _totalDuration > 0)
            _progressArc.SweepAngle = 360f * (float)(remaining / _totalDuration);

        // Glow tint when nearly done
        if (_progressArc != null && remaining <= 5)
            _progressArc.Stroke = new SolidColorBrush(Color.FromArgb(200, 0, 230, 230));
    }

    protected override void OnClosed(EventArgs e)
    {
        _uiTimer.Stop();
        base.OnClosed(e);
    }
}
