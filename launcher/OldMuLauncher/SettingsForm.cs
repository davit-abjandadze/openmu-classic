namespace OldMu.Launcher;

/// <summary>
/// Edits the game options: resolution, full screen, the remembered account and the music.
/// </summary>
public sealed class SettingsForm : Form
{
    private static readonly Color Back = Color.FromArgb(22, 20, 26);
    private static readonly Color Field = Color.FromArgb(34, 31, 39);
    private static readonly Color TextColor = Color.FromArgb(226, 214, 190);
    private static readonly Color Gold = Color.FromArgb(200, 160, 90);

    private readonly ClientConfig config;
    private readonly ComboBox resolution = new() { DropDownStyle = ComboBoxStyle.DropDownList, FormattingEnabled = true, FlatStyle = FlatStyle.Flat, BackColor = Field, ForeColor = TextColor, Width = 200 };
    private readonly CheckBox fullScreen = new() { Text = "Full screen", ForeColor = TextColor, AutoSize = true };
    private readonly CheckBox rememberAccount = new() { Text = "Remember my account on the login screen", ForeColor = TextColor, AutoSize = true };
    private readonly CheckBox music = new() { Text = "Play music in the game", ForeColor = TextColor, AutoSize = true };

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsForm"/> class.
    /// </summary>
    public SettingsForm(ClientConfig config)
    {
        this.config = config;
        this.Text = "Old MU - Settings";
        this.BackColor = Back;
        this.ForeColor = TextColor;
        this.Font = new Font("Segoe UI", 10f);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.ShowInTaskbar = false;
        this.StartPosition = FormStartPosition.CenterParent;
        this.AutoScaleMode = AutoScaleMode.Dpi;
        this.AutoSize = true;
        this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        this.Padding = new Padding(16);

        var sizes = ClientConfig.Resolutions.ToList();
        if (!sizes.Contains(config.Resolution))
        {
            sizes.Add(config.Resolution);
        }

        foreach (var size in sizes)
        {
            this.resolution.Items.Add(size);
        }

        this.resolution.Format += (_, e) => e.Value = e.ListItem is Size size ? $"{size.Width} x {size.Height}" : e.Value;

        var table = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Dock = DockStyle.Fill };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        AddRow(table, "Resolution", this.resolution);
        AddRow(table, string.Empty, this.fullScreen);
        AddRow(table, string.Empty, this.rememberAccount);
        AddRow(table, string.Empty, this.music);

        var hint = new Label
        {
            Text = "The account is remembered when you log in with it once. The password is never stored.",
            ForeColor = Color.FromArgb(150, 145, 135),
            AutoSize = true,
            MaximumSize = new Size(420, 0),
            Margin = new Padding(3, 10, 3, 3),
        };
        table.Controls.Add(hint, 0, table.RowCount);
        table.SetColumnSpan(hint, 2);
        table.RowCount++;

        var ok = CreateButton("Save");
        ok.Click += (_, _) => this.Apply();
        var cancel = CreateButton("Cancel");
        cancel.DialogResult = DialogResult.Cancel;
        var buttons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, 12, 0, 0) };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);
        table.Controls.Add(buttons, 0, table.RowCount);
        table.SetColumnSpan(buttons, 2);
        table.RowCount++;

        this.Controls.Add(table);
        this.AcceptButton = ok;
        this.CancelButton = cancel;

        this.resolution.SelectedItem = config.Resolution;
        this.fullScreen.Checked = config.FullScreen;
        this.rememberAccount.Checked = config.RememberAccount;
        this.music.Checked = config.Music;
    }

    private static Button CreateButton(string text)
    {
        var button = new Button { Text = text, AutoSize = true, FlatStyle = FlatStyle.Flat, BackColor = Field, ForeColor = TextColor, Padding = new Padding(8, 2, 8, 2) };
        button.FlatAppearance.BorderColor = Gold;
        return button;
    }

    private static void AddRow(TableLayoutPanel table, string label, Control control)
    {
        table.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 12, 3) }, 0, table.RowCount);
        table.Controls.Add(control, 1, table.RowCount);
        table.RowCount++;
    }

    private void Apply()
    {
        this.config.Resolution = (Size)this.resolution.SelectedItem!;
        this.config.FullScreen = this.fullScreen.Checked;
        this.config.RememberAccount = this.rememberAccount.Checked;
        this.config.Music = this.music.Checked;
        if (this.config.Save() is { } error)
        {
            MessageBox.Show(this, error, this.Text);
            return;
        }

        this.DialogResult = DialogResult.OK;
    }
}
