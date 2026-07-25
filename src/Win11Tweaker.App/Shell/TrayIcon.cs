using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using Forms = System.Windows.Forms;

namespace Win11Tweaker.App.Shell;

public sealed class TrayIcon : IDisposable
{
    readonly Forms.NotifyIcon icon;

    public TrayIcon(Action onOpen, Action onExit)
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Открыть Win11 Tweaker", null, (_, _) => onOpen());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Выход", null, (_, _) => onExit());

        icon = new Forms.NotifyIcon
        {
            Icon = MakeIcon(),
            Text = "Win11 Tweaker",
            Visible = false,
            ContextMenuStrip = menu
        };

        icon.MouseClick += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left)
                onOpen();
        };
    }

    public void Show(string? balloonTitle, string? balloonText)
    {
        icon.Visible = true;
        if (balloonTitle is not null)
        {
            icon.BalloonTipTitle = balloonTitle;
            icon.BalloonTipText = balloonText ?? string.Empty;
            icon.ShowBalloonTip(4000);
        }
    }

    public void Hide() => icon.Visible = false;

    static Icon MakeIcon()
    {
        try
        {
            var res = Application.GetResourceStream(new Uri("Assets/win11tweaker.ico", UriKind.Relative));
            if (res is not null)
            {
                using var stream = res.Stream;
                return new Icon(stream, new System.Drawing.Size(16, 16));
            }
        }
        catch (Exception)
        {
        }

        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(Color.FromArgb(0xFF, 0x6E, 0x7B, 0xE8));
            g.FillEllipse(brush, 7, 7, 18, 18);
        }
        return Icon.FromHandle(bmp.GetHicon());
    }

    public void Dispose()
    {
        icon.Visible = false;
        icon.Dispose();
    }
}
