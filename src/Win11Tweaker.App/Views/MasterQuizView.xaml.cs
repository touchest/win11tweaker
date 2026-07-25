using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Win11Tweaker.App.Views;

public partial class MasterQuizView : UserControl
{
    double builtW, builtH;

    public MasterQuizView() => InitializeComponent();

    void IconFieldLoaded(object sender, RoutedEventArgs e) => BuildField();

    void IconFieldSizeChanged(object sender, SizeChangedEventArgs e) => BuildField();

    void BuildField()
    {
        var canvas = IconField;
        double w = canvas.ActualWidth, h = canvas.ActualHeight;
        if (w < 60 || h < 60)
            return;

        if (Math.Abs(w - builtW) < 24 && Math.Abs(h - builtH) < 24 && canvas.Children.Count > 0)
            return;
        builtW = w;
        builtH = h;
        canvas.Children.Clear();

        const double gap = 26;
        double cx = w / 2, cy = h / 2;
        double maxD = Math.Sqrt(cx * cx + cy * cy);

        var inkBrush = new SolidColorBrush(((SolidColorBrush)FindResource("Ink")).Color);
        var irisBrush = new SolidColorBrush(((SolidColorBrush)FindResource("Accent")).Color);
        inkBrush.Freeze();
        irisBrush.Freeze();

        for (double y = gap; y < h - gap / 2; y += gap)
        {
            for (double x = gap; x < w - gap / 2; x += gap)
            {
                double d = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / maxD;
                double radial = Smooth(Math.Clamp((d - 0.14) / 0.72, 0, 1));

                double light = Math.Clamp(0.5 * (x / w) + 0.5 * (1 - y / h), 0, 1);

                double vis = radial * (0.32 + 0.68 * light);
                if (vis <= 0.015)
                    continue;

                double r = 1.3 + 1.1 * light;
                double op = 0.16 * vis;
                bool iris = light > 0.60;

                var dot = new Ellipse
                {
                    Width = r * 2,
                    Height = r * 2,
                    Fill = iris ? irisBrush : inkBrush,
                    Opacity = op,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(dot, x - r);
                Canvas.SetTop(dot, y - r);
                canvas.Children.Add(dot);
            }
        }

        var breathe = new DoubleAnimation
        {
            From = 0.8,
            To = 1.0,
            Duration = TimeSpan.FromSeconds(7),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        canvas.BeginAnimation(OpacityProperty, breathe);
    }

    static double Smooth(double t) => t * t * (3 - 2 * t);
}
