using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Yomi.App.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _icon;
    private readonly ContextMenuStrip _contextMenu;

    public event Action? ToggleVisibilityRequested;
    public event Action? SettingsRequested;
    public event Action? ExitRequested;

    public TrayIconService()
    {
        _contextMenu = new ContextMenuStrip
        {
            ShowImageMargin = false,
            ShowCheckMargin = false,
            Padding = new Padding(10, 10, 10, 10),
            Font = new Font("Segoe UI", 9f),
        };
        _contextMenu.Opening += (_, _) => ApplyTheme();
        _contextMenu.SizeChanged += (_, _) => UpdateMenuRegion();
        _contextMenu.Items.Add("表示/非表示切替", null, (_, _) => ToggleVisibilityRequested?.Invoke());
        _contextMenu.Items.Add("設定", null, (_, _) => SettingsRequested?.Invoke());
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add("終了", null, (_, _) => ExitRequested?.Invoke());
        foreach (ToolStripItem item in _contextMenu.Items)
            if (item is ToolStripMenuItem)
            {
                item.Padding = new Padding(16, 12, 24, 12);
                item.TextAlign = ContentAlignment.MiddleLeft;
                item.AutoSize = true;
                item.Margin = new Padding(0, 1, 0, 1);
            }

        using var iconStream = typeof(TrayIconService).Assembly.GetManifestResourceStream("Yomi.App.Assets.wave-icon.ico")
            ?? throw new InvalidOperationException("Tray icon resource was not found.");
        _icon = new Icon(iconStream);
        _notifyIcon = new NotifyIcon
        {
            Icon = _icon,
            Text = "yomi",
            Visible = true,
            ContextMenuStrip = _contextMenu,
        };
        _notifyIcon.DoubleClick += (_, _) => ToggleVisibilityRequested?.Invoke();
    }

    private void UpdateMenuRegion()
    {
        if (_contextMenu.Width <= 0 || _contextMenu.Height <= 0) return;
        var radius = Math.Max(1, (int)Math.Round(8 * _contextMenu.DeviceDpi / 96d));
        using var path = RoundedRectangle(new Rectangle(0, 0, _contextMenu.Width, _contextMenu.Height), radius);
        var oldRegion = _contextMenu.Region;
        _contextMenu.Region = new Region(path);
        oldRegion?.Dispose();
    }

    private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private void ApplyTheme()
    {
        var light = SystemInformation.HighContrast ||
            (int?)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1) != 0;
        var background = SystemInformation.HighContrast ? SystemColors.Menu : light ? System.Drawing.Color.FromArgb(249, 249, 249) : System.Drawing.Color.FromArgb(43, 43, 43);
        var foreground = SystemInformation.HighContrast ? SystemColors.MenuText : light ? System.Drawing.Color.FromArgb(32, 32, 32) : System.Drawing.Color.White;
        var hover = SystemInformation.HighContrast ? SystemColors.Highlight : light ? System.Drawing.Color.FromArgb(232, 232, 232) : System.Drawing.Color.FromArgb(62, 62, 62);
        _contextMenu.BackColor = background;
        _contextMenu.ForeColor = foreground;
        _contextMenu.Renderer = new FluentMenuRenderer(background, hover, light);
        // Assign after changing the renderer, which resets the menu's padding.
        var inset = Math.Max(1, (int)Math.Round(10 * _contextMenu.DeviceDpi / 96d));
        _contextMenu.Padding = new Padding(inset);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _icon.Dispose();
        _contextMenu.Dispose();
    }

    private sealed class FluentMenuRenderer(System.Drawing.Color background, System.Drawing.Color hover, bool light) : ToolStripProfessionalRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using var brush = new SolidBrush(background);
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using var pen = new System.Drawing.Pen(light ? System.Drawing.Color.FromArgb(215, 215, 215) : System.Drawing.Color.FromArgb(70, 70, 70));
            var radius = Math.Max(1, (int)Math.Round(8 * e.ToolStrip.DeviceDpi / 96d));
            using var path = RoundedRectangle(new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1), radius);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawPath(pen, path);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected && !e.Item.Pressed) return;
            var rect = new Rectangle(2, 1, e.Item.Width - 5, e.Item.Height - 3);
            using var path = new GraphicsPath();
            int diameter = Math.Max(8, (int)Math.Round(10 * (e.ToolStrip?.DeviceDpi ?? 96) / 96d));
            path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            using var brush = new SolidBrush(hover);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.FillPath(brush, path);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            var inset = (int)Math.Round(16 * (e.ToolStrip?.DeviceDpi ?? 96) / 96d);
            var bounds = new Rectangle(inset, 0, Math.Max(0, e.Item.Width - inset * 2), e.Item.Height);
            var color = !e.Item.Enabled ? SystemColors.GrayText :
                SystemInformation.HighContrast && e.Item.Selected ? SystemColors.HighlightText : e.Item.ForeColor;
            TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont, bounds, color,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using var pen = new System.Drawing.Pen(light ? System.Drawing.Color.FromArgb(220, 220, 220) : System.Drawing.Color.FromArgb(75, 75, 75));
            e.Graphics.DrawLine(pen, 8, e.Item.Height / 2, e.Item.Width - 8, e.Item.Height / 2);
        }
    }
}
