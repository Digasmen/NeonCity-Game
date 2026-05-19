namespace PowerMonitor;

partial class MainForm
{
    private void InitializeComponent()
    {
        // ── form ──────────────────────────────────────────────────────────
        Text            = "NEONCITY  //  POWER MONITOR";
        Size            = new Size(820, 640);
        MinimumSize     = new Size(640, 480);
        BackColor       = BgDeep;
        ForeColor       = NeonCyan;
        Font            = new Font("Consolas", 9.5f, FontStyle.Regular);
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition   = FormStartPosition.CenterScreen;

        // ── outer padding panel ───────────────────────────────────────────
        var root = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            BackColor   = BgDeep,
            Padding     = new Padding(14),
            RowCount    = 3,
            ColumnCount = 1,
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // header
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // grid
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // footer
        Controls.Add(root);

        // ── header: three stat tiles ──────────────────────────────────────
        var header = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            BackColor   = BgDeep,
            ColumnCount = 3,
            RowCount    = 1,
            Height      = 110,
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4f));
        root.Controls.Add(header, 0, 0);

        _lblCpuWatts   = MakeTile("CPU",   NeonCyan);
        _lblGpuWatts   = MakeTile("GPU",   NeonPurp);
        _lblTotalWatts = MakeTile("TOTAL", NeonAmber);

        header.Controls.Add(WrapTile(_lblCpuWatts,   "CPU"),   0, 0);
        header.Controls.Add(WrapTile(_lblGpuWatts,   "GPU"),   1, 0);
        header.Controls.Add(WrapTile(_lblTotalWatts, "TOTAL"), 2, 0);

        // ── sensor grid ───────────────────────────────────────────────────
        _grid = new DataGridView
        {
            Dock                  = DockStyle.Fill,
            BackgroundColor       = BgPanel,
            ForeColor             = NeonCyan,
            GridColor             = GridLine,
            BorderStyle           = BorderStyle.None,
            RowHeadersVisible     = false,
            AllowUserToAddRows    = false,
            AllowUserToDeleteRows = false,
            ReadOnly              = true,
            SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect           = false,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight   = 26,
            AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
            ScrollBars            = ScrollBars.Vertical,
        };

        _grid.ColumnHeadersDefaultCellStyle.BackColor  = BgPanel;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor  = TextDim;
        _grid.ColumnHeadersDefaultCellStyle.Font       = new Font("Consolas", 8.5f, FontStyle.Regular);
        _grid.DefaultCellStyle.BackColor               = BgRow;
        _grid.DefaultCellStyle.ForeColor               = NeonCyan;
        _grid.DefaultCellStyle.SelectionBackColor      = Color.FromArgb(30, 0, 180, 255);
        _grid.DefaultCellStyle.SelectionForeColor      = Color.White;
        _grid.DefaultCellStyle.Padding                 = new Padding(4, 2, 4, 2);
        _grid.AlternatingRowsDefaultCellStyle.BackColor = BgRowAlt;

        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "COMPONENT",  FillWeight = 40 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SENSOR",     FillWeight = 35 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "WATTS",      FillWeight = 15, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "UNIT",       FillWeight = 10 });

        var gridWrapper = new Panel
        {
            Dock        = DockStyle.Fill,
            BackColor   = BgPanel,
            Padding     = new Padding(0, 8, 0, 0),
        };
        gridWrapper.Controls.Add(_grid);
        root.Controls.Add(gridWrapper, 0, 1);

        // ── footer ────────────────────────────────────────────────────────
        var footer = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 1,
            BackColor   = BgDeep,
            Height      = 28,
            Padding     = new Padding(0, 6, 0, 0),
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _lblAdminWarn = new Label
        {
            Text      = "  No sensors found — run as Administrator for full hardware access.",
            ForeColor = Color.FromArgb(255, 80, 80),
            AutoSize  = true,
            Visible   = false,
        };

        _lblTimestamp = new Label
        {
            Text      = "—",
            ForeColor = TextDim,
            AutoSize  = true,
            TextAlign = ContentAlignment.MiddleRight,
        };

        footer.Controls.Add(_lblAdminWarn,  0, 0);
        footer.Controls.Add(_lblTimestamp,  1, 0);
        root.Controls.Add(footer, 0, 2);
    }

    private static Label MakeTile(string _, Color accent)
    {
        return new Label
        {
            Text      = "— W",
            ForeColor = accent,
            Font      = new Font("Consolas", 22f, FontStyle.Bold),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize  = false,
        };
    }

    private static Panel WrapTile(Label valueLabel, string title)
    {
        var titleLabel = new Label
        {
            Text      = title,
            ForeColor = TextDim,
            Font      = new Font("Consolas", 8f, FontStyle.Regular),
            Dock      = DockStyle.Top,
            Height    = 22,
            TextAlign = ContentAlignment.BottomCenter,
        };

        var panel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = BgPanel,
            Margin    = new Padding(0, 0, 8, 8),
        };

        panel.Controls.Add(valueLabel);
        panel.Controls.Add(titleLabel);
        return panel;
    }
}
