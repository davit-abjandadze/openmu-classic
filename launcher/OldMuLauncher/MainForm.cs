namespace OldMu.Launcher;

using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

/// <summary>
/// The borderless launcher window. The artwork is one picture; the controls are hot spots on top of it.
/// </summary>
public sealed class MainForm : Form
{
    private const int ArtWidth = 1536;
    private const int ArtHeight = 1024;

    private static readonly Color PanelFill = Color.FromArgb(18, 15, 21);
    private static readonly Color ServerBoxFill = Color.FromArgb(15, 16, 22);
    private static readonly Color Online = Color.FromArgb(52, 214, 60);
    private static readonly Color Offline = Color.FromArgb(226, 58, 50);
    private static readonly Color Muted = Color.FromArgb(150, 150, 160);
    private static readonly Color Parchment = Color.FromArgb(226, 214, 190);

    /// <summary>Hot spots in artwork coordinates.</summary>
    private static readonly Dictionary<Spot, Rectangle> Spots = new()
    {
        [Spot.Minimize] = Rectangle.FromLTRB(1424, 14, 1464, 50),
        [Spot.Close] = Rectangle.FromLTRB(1474, 14, 1514, 50),
        [Spot.Settings] = Rectangle.FromLTRB(40, 840, 252, 904),
        [Spot.Website] = Rectangle.FromLTRB(272, 840, 476, 904),
        [Spot.Discord] = Rectangle.FromLTRB(496, 840, 698, 904),
        [Spot.Update] = Rectangle.FromLTRB(716, 840, 886, 904),
        [Spot.Start] = Rectangle.FromLTRB(1080, 784, 1490, 886),
        [Spot.Server] = Rectangle.FromLTRB(1110, 630, 1474, 682),
    };

    private static readonly Rectangle ServerNameArea = Rectangle.FromLTRB(1162, 636, 1432, 677);
    private static readonly Rectangle SignalArea = Rectangle.FromLTRB(1114, 636, 1156, 677);
    private static readonly Rectangle StatusArea = Rectangle.FromLTRB(1204, 698, 1380, 734);
    private static readonly Rectangle FooterMessageArea = Rectangle.FromLTRB(560, 956, 1500, 1006);

    private readonly LauncherSettings settings;
    private bool updating;
    private readonly Image artwork;
    private readonly System.Windows.Forms.Timer statusTimer = new() { Interval = 5000 };
    private readonly CancellationTokenSource disposing = new();

    private Bitmap? background;
    private float scale = 1;
    private Spot hovered = Spot.None;
    private Spot pressed = Spot.None;
    private bool? online;
    private bool checking;
    private string? message;
    private bool messageIsError;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainForm"/> class.
    /// </summary>
    public MainForm(LauncherSettings settings)
    {
        this.settings = settings;
        using (var stream = typeof(MainForm).Assembly.GetManifestResourceStream("background.png")!)
        {
            this.artwork = Image.FromStream(stream);
        }

        using (var iconStream = typeof(MainForm).Assembly.GetManifestResourceStream("app.ico")!)
        {
            this.Icon = new Icon(iconStream);
        }

        this.Text = "Old MU";
        this.FormBorderStyle = FormBorderStyle.None;
        this.StartPosition = FormStartPosition.Manual;
        this.AutoScaleMode = AutoScaleMode.None;
        this.DoubleBuffered = true;
        this.KeyPreview = true;
        this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);

        this.FitToScreen(Screen.FromPoint(Cursor.Position));
        this.statusTimer.Tick += (_, _) => _ = this.RefreshStatusAsync();
        this.UpdateClientMessage();
    }

    private enum Spot
    {
        None,
        Minimize,
        Close,
        Settings,
        Website,
        Discord,
        Update,
        Start,
        Server,
    }

    /// <inheritdoc />
    protected override CreateParams CreateParams
    {
        get
        {
            const int csDropShadow = 0x00020000;
            const int wsMinimizeBox = 0x00020000;
            var parameters = base.CreateParams;
            parameters.ClassStyle |= csDropShadow;
            parameters.Style |= wsMinimizeBox; // allows minimizing through the taskbar
            return parameters;
        }
    }

    /// <inheritdoc />
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        this.statusTimer.Start();
        _ = this.RefreshStatusAsync();
        _ = this.UpdateGameAsync();
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.DrawImageUnscaled(this.background!, 0, 0);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        this.PaintServer(g);
        this.PaintStatus(g);
        this.PaintFooterMessage(g);

        var highlighted = this.pressed != Spot.None ? this.pressed : this.hovered;
        if (highlighted != Spot.None && highlighted != Spot.Server)
        {
            this.PaintHighlight(g, highlighted, this.pressed == highlighted);
        }
        else if (highlighted == Spot.Server)
        {
            using var pen = new Pen(Color.FromArgb(170, 230, 190, 110), 1.5f * this.scale);
            g.DrawRectangle(pen, this.ToClient(Spots[Spot.Server]));
        }
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var spot = this.HitTest(e.Location);
        if (spot != this.hovered)
        {
            this.hovered = spot;
            this.Cursor = spot == Spot.None ? Cursors.Default : Cursors.Hand;
            this.Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        this.hovered = Spot.None;
        this.Invalidate();
    }

    /// <inheritdoc />
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        var spot = this.HitTest(e.Location);
        if (spot == Spot.None)
        {
            // Dragging the artwork moves the window.
            NativeMethods.ReleaseCapture();
            NativeMethods.SendMessage(this.Handle, NativeMethods.WmNcLButtonDown, NativeMethods.HtCaption, 0);
            return;
        }

        this.pressed = spot;
        this.Invalidate();
    }

    /// <inheritdoc />
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        var spot = this.pressed;
        this.pressed = Spot.None;
        this.Invalidate();
        if (spot != Spot.None && spot == this.HitTest(e.Location))
        {
            this.Activate(spot);
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Enter)
        {
            this.Activate(Spot.Start);
        }
        else if (e.KeyCode == Keys.Escape)
        {
            this.Close();
        }
    }

    /// <inheritdoc />
    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        this.FitToScreen(Screen.FromControl(this));
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.disposing.Cancel();
            this.statusTimer.Dispose();
            this.background?.Dispose();
            this.artwork.Dispose();
            this.disposing.Dispose();
        }

        base.Dispose(disposing);
    }

    private static void OpenUrl(string url)
    {
        // A link which isn't configured (e.g. no Discord yet) does nothing.
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            MessageBox.Show($"'{url}' could not be opened: {ex.Message}", "Old MU");
        }
    }

    private static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private void FitToScreen(Screen screen)
    {
        var area = screen.WorkingArea;
        this.scale = Math.Min(1f, Math.Min(area.Width * 0.85f / ArtWidth, area.Height * 0.85f / ArtHeight));
        var size = new Size((int)Math.Round(ArtWidth * this.scale), (int)Math.Round(ArtHeight * this.scale));

        var scaled = new Bitmap(size.Width, size.Height);
        using (var g = Graphics.FromImage(scaled))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(this.artwork, new Rectangle(Point.Empty, size));
            g.ScaleTransform(this.scale, this.scale);
            this.PaintUpdateLabel(g);
        }

        this.background?.Dispose();
        this.background = scaled;
        this.ClientSize = size;
        if (!this.Visible)
        {
            this.Location = new Point(area.Left + ((area.Width - size.Width) / 2), area.Top + ((area.Height - size.Height) / 2));
        }

        this.Invalidate();
    }

    private void PaintUpdateLabel(Graphics g)
    {
        // Reuse the unlettered strip of the original button to retain its vertical shading and frame.
        g.DrawImage(this.artwork, new Rectangle(735, 852, 130, 39), new Rectangle(790, 852, 7, 39), GraphicsUnit.Pixel);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        var ink = Color.FromArgb(245, 243, 242);
        using var pen = new Pen(ink, 3);
        using var brush = new SolidBrush(ink);
        g.DrawArc(pen, 750, 862, 22, 22, 0, 310);
        g.FillPolygon(brush, [new Point(764, 868), new Point(773, 868), new Point(773, 859)]);
        using var font = new Font("Times New Roman", 23, FontStyle.Regular, GraphicsUnit.Pixel);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString("Update", font, brush, new RectangleF(780, 853, 80, 39), format);
    }

    private Rectangle ToClient(Rectangle art) => Rectangle.Round(new RectangleF(art.X * this.scale, art.Y * this.scale, art.Width * this.scale, art.Height * this.scale));

    private Font ArtFont(float artPixels, FontStyle style = FontStyle.Regular) => new("Georgia", artPixels * this.scale, style, GraphicsUnit.Pixel);

    private Spot HitTest(Point clientPoint)
    {
        var art = new Point((int)(clientPoint.X / this.scale), (int)(clientPoint.Y / this.scale));
        foreach (var (spot, rectangle) in Spots)
        {
            if (rectangle.Contains(art))
            {
                return spot;
            }
        }

        return Spot.None;
    }

    private void PaintHighlight(Graphics g, Spot spot, bool isPressed)
    {
        var bounds = (RectangleF)this.ToClient(Spots[spot]);
        bounds.Inflate(-2 * this.scale, -2 * this.scale);
        var radius = (spot == Spot.Start ? 14 : 6) * this.scale;
        using var path = RoundedRectangle(bounds, radius);

        var fill = isPressed
            ? Color.FromArgb(70, 0, 0, 0)
            : spot == Spot.Start ? Color.FromArgb(50, 255, 220, 140) : Color.FromArgb(38, 255, 210, 130);
        using var brush = new SolidBrush(fill);
        g.FillPath(brush, path);

        using var glow = new Pen(Color.FromArgb(isPressed ? 90 : 190, 240, 196, 100), 2f * this.scale);
        g.DrawPath(glow, path);
    }

    private void PaintServer(Graphics g)
    {
        var server = this.settings.Server;
        var nameArea = this.ToClient(ServerNameArea);
        using (var fill = new SolidBrush(ServerBoxFill))
        {
            g.FillRectangle(fill, nameArea);
        }

        using (var font = this.ArtFont(25))
        using (var brush = new SolidBrush(Parchment))
        {
            var format = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
            g.DrawString(server.Name, font, brush, nameArea, format);
        }

        if (this.online == false)
        {
            // Grey out the signal bars of the artwork.
            using var shade = new SolidBrush(Color.FromArgb(185, ServerBoxFill));
            g.FillRectangle(shade, this.ToClient(SignalArea));
        }
    }

    private void PaintStatus(Graphics g)
    {
        var area = this.ToClient(StatusArea);
        using (var fill = new SolidBrush(PanelFill))
        {
            g.FillRectangle(fill, area);
        }

        var (text, color) = this.online switch
        {
            true => ("Online", Online),
            false => ("Offline", Offline),
            null => ("Checking...", Muted),
        };

        using var font = this.ArtFont(27);
        using var brush = new SolidBrush(color);
        var format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
        g.DrawString(text, font, brush, area, format);
    }

    private void PaintFooterMessage(Graphics g)
    {
        if (string.IsNullOrEmpty(this.message))
        {
            return;
        }

        using var font = this.ArtFont(20);
        using var brush = new SolidBrush(this.messageIsError ? Color.FromArgb(240, 120, 100) : Color.FromArgb(185, 175, 155));
        var format = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisPath, FormatFlags = StringFormatFlags.NoWrap };
        g.DrawString(this.message, font, brush, this.ToClient(FooterMessageArea), format);
    }

    private void ShowMessage(string? text, bool isError)
    {
        this.message = text;
        this.messageIsError = isError;
        this.Invalidate();
    }

    private void UpdateClientMessage()
    {
        if (File.Exists(this.settings.ClientFullPath))
        {
            this.ShowMessage(null, false);
        }
        else
        {
            this.ShowMessage($"Client not found: {this.settings.ClientFullPath}", true);
        }
    }

    private async Task RefreshStatusAsync()
    {
        if (this.checking)
        {
            return;
        }

        this.checking = true;
        try
        {
            var server = this.settings.Server;
            var result = await GameStarter.IsOnlineAsync(server, this.disposing.Token).ConfigureAwait(true);
            if (!this.IsDisposed && ReferenceEquals(server, this.settings.Server) && result != this.online)
            {
                this.online = result;
                this.Invalidate();
            }
        }
        finally
        {
            this.checking = false;
        }
    }

    private void Activate(Spot spot)
    {
        if (this.updating && spot != Spot.Minimize)
        {
            return;
        }

        switch (spot)
        {
            case Spot.Minimize:
                this.WindowState = FormWindowState.Minimized;
                break;
            case Spot.Close:
                this.Close();
                break;
            case Spot.Update:
                _ = this.UpdateGameAsync();
                break;
            case Spot.Website:
                OpenUrl(this.settings.WebsiteUrl);
                break;
            case Spot.Discord:
                OpenUrl(this.settings.DiscordUrl);
                break;
            case Spot.Settings:
                this.OpenSettings();
                break;
            case Spot.Server:
                this.ShowServerMenu();
                break;
            case Spot.Start:
                this.StartGame();
                break;
        }
    }

    private void StartGame()
    {
        if (File.Exists(Path.Combine(Path.GetDirectoryName(this.settings.ClientFullPath)!, ClientUpdater.PendingFile)))
        {
            this.ShowMessage("Press Update to finish the interrupted update.", true);
            return;
        }

        if (GameStarter.Start(this.settings) is { } error)
        {
            this.ShowMessage(error, true);
            return;
        }

        this.ShowMessage("Starting the game...", false);
        if (this.settings.CloseAfterStart)
        {
            this.Close();
        }
        else
        {
            this.WindowState = FormWindowState.Minimized;
        }
    }

    private async Task UpdateGameAsync()
    {
        if (this.updating)
        {
            return;
        }

        this.updating = true;
        try
        {
            var running = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(this.settings.ClientFullPath));
            foreach (var process in running)
            {
                process.Dispose();
            }

            if (running.Length > 0)
            {
                this.ShowMessage("Close the game, then press Update.", true);
                return;
            }

            this.ShowMessage("Checking for updates...", false);
            var progress = new Progress<string>(text => this.ShowMessage(text, false));
            var count = await Task.Run(() => ClientUpdater.ApplyAsync(this.settings.WebsiteUrl,
                Path.GetDirectoryName(this.settings.ClientFullPath)!, progress));
            this.ShowMessage(count == 0 ? "Your client is up to date." : $"Updated {count} files. Ready to play.", false);
        }
        catch (Exception ex)
        {
            this.ShowMessage(ex.Message, true);
        }
        finally
        {
            this.updating = false;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (this.updating && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
        }

        base.OnFormClosing(e);
    }

    private void OpenSettings()
    {
        var (config, error) = ClientConfig.Load(this.settings.ClientFullPath);
        if (config is null)
        {
            MessageBox.Show(this, error, "Old MU");
            return;
        }

        using var dialog = new SettingsForm(config);
        dialog.ShowDialog(this);
    }

    private void ShowServerMenu()
    {
        var menu = new ContextMenuStrip
        {
            BackColor = Color.FromArgb(24, 22, 28),
            ForeColor = Parchment,
            ShowImageMargin = false,
            Font = new Font("Georgia", 11f),
            Renderer = new ToolStripProfessionalRenderer(new DarkMenuColors()),
        };

        for (var i = 0; i < this.settings.Servers.Count; i++)
        {
            var index = i;
            var server = this.settings.Servers[i];
            var item = new ToolStripMenuItem($"{server.Name}   ({server.Host}:{server.Port})")
            {
                Checked = index == this.settings.SelectedServer,
                ForeColor = Parchment,
            };
            item.Click += (_, _) =>
            {
                this.settings.SelectedServer = index;
                this.settings.Save();
                this.online = null;
                this.Invalidate();
                _ = this.RefreshStatusAsync();
            };
            menu.Items.Add(item);
        }

        menu.Closed += (_, _) => this.BeginInvoke(menu.Dispose);

        var box = this.ToClient(Spots[Spot.Server]);
        menu.Show(this, new Point(box.Left, box.Bottom));
    }

    private sealed class DarkMenuColors : ProfessionalColorTable
    {
        public override Color MenuItemSelected => Color.FromArgb(70, 52, 30);

        public override Color MenuItemBorder => Color.FromArgb(200, 160, 90);

        public override Color MenuBorder => Color.FromArgb(120, 95, 55);

        public override Color ToolStripDropDownBackground => Color.FromArgb(24, 22, 28);

        public override Color SeparatorDark => Color.FromArgb(90, 75, 50);

        public override Color CheckBackground => Color.FromArgb(70, 52, 30);

        public override Color CheckSelectedBackground => Color.FromArgb(90, 66, 36);
    }

    private static class NativeMethods
    {
        public const int WmNcLButtonDown = 0xA1;
        public const int HtCaption = 0x2;

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
    }
}
