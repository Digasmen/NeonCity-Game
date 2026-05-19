using LibreHardwareMonitor.Hardware;

namespace PowerMonitor;

public partial class MainForm : Form
{
    private readonly HardwareMonitorService _monitor;
    private readonly System.Windows.Forms.Timer _timer;

    // Neon colour palette
    private static readonly Color BgDeep    = Color.FromArgb(10,  10,  26);
    private static readonly Color BgPanel   = Color.FromArgb(16,  16,  38);
    private static readonly Color BgRow     = Color.FromArgb(20,  20,  46);
    private static readonly Color BgRowAlt  = Color.FromArgb(14,  14,  32);
    private static readonly Color NeonCyan  = Color.FromArgb(0,   230, 255);
    private static readonly Color NeonPurp  = Color.FromArgb(180,  0,  255);
    private static readonly Color NeonGreen = Color.FromArgb(57,  255,  20);
    private static readonly Color NeonAmber = Color.FromArgb(255, 170,   0);
    private static readonly Color TextDim   = Color.FromArgb(120, 140, 180);
    private static readonly Color GridLine  = Color.FromArgb(30,  40,  70);

    private Label _lblCpuWatts   = null!;
    private Label _lblGpuWatts   = null!;
    private Label _lblTotalWatts = null!;
    private Label _lblTimestamp  = null!;
    private Label _lblAdminWarn  = null!;
    private DataGridView _grid   = null!;

    public MainForm()
    {
        InitializeComponent();
        _monitor = new HardwareMonitorService();
        _timer   = new System.Windows.Forms.Timer { Interval = 1000 };
        _timer.Tick += (_, _) => Refresh_Readings();
        _timer.Start();
        Refresh_Readings();
    }

    private void Refresh_Readings()
    {
        List<PowerReading> readings;
        try
        {
            readings = _monitor.GetPowerReadings();
        }
        catch
        {
            _lblAdminWarn.Visible = true;
            return;
        }

        // ── aggregate primary components ──────────────────────────────────
        float cpuTotal  = Sum(readings, HardwareType.Cpu,  "Package");
        float gpuTotal  = Sum(readings, null,              "GPU Package", HardwareType.GpuNvidia, HardwareType.GpuAmd, HardwareType.GpuIntel);
        float sysTotal  = readings.Sum(r => r.Watts);

        _lblCpuWatts.Text   = cpuTotal  > 0 ? $"{cpuTotal:F1} W"  : "— W";
        _lblGpuWatts.Text   = gpuTotal  > 0 ? $"{gpuTotal:F1} W"  : "— W";
        _lblTotalWatts.Text = sysTotal  > 0 ? $"{sysTotal:F1} W"  : "— W";
        _lblTimestamp.Text  = $"Updated {DateTime.Now:HH:mm:ss}";

        // Colour-code totals by power band
        _lblCpuWatts.ForeColor   = WattsColor(cpuTotal);
        _lblGpuWatts.ForeColor   = WattsColor(gpuTotal);
        _lblTotalWatts.ForeColor = WattsColor(sysTotal);

        // ── populate grid ─────────────────────────────────────────────────
        _grid.SuspendLayout();
        _grid.Rows.Clear();
        foreach (var r in readings.OrderBy(r => r.HardwareType).ThenBy(r => r.ComponentName))
        {
            int row = _grid.Rows.Add(r.ComponentName, r.SensorName, $"{r.Watts:F2}", "W");
            _grid.Rows[row].DefaultCellStyle.BackColor =
                row % 2 == 0 ? BgRow : BgRowAlt;
        }
        _grid.ResumeLayout();

        _lblAdminWarn.Visible = readings.Count == 0;
    }

    // Sum sensors whose name contains a keyword, optionally filtered by hardware type(s)
    private static float Sum(
        List<PowerReading> readings,
        HardwareType? requiredType,
        string nameContains,
        params HardwareType[] extraTypes)
    {
        return readings
            .Where(r =>
            {
                bool typeOk = requiredType.HasValue
                    ? r.HardwareType == requiredType.Value
                    : extraTypes.Length == 0 || extraTypes.Contains(r.HardwareType);
                bool nameOk = r.SensorName.Contains(nameContains, StringComparison.OrdinalIgnoreCase);
                return typeOk && nameOk;
            })
            .Sum(r => r.Watts);
    }

    private static Color WattsColor(float w) => w switch
    {
        <= 0   => TextDim,
        < 50   => NeonGreen,
        < 150  => NeonCyan,
        < 250  => NeonAmber,
        _      => Color.FromArgb(255, 70, 70)
    };

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer.Stop();
        _monitor.Dispose();
        base.OnFormClosed(e);
    }
}
