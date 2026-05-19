using LibreHardwareMonitor.Hardware;

namespace PowerMonitor;

public record PowerReading(
    string ComponentName,
    string SensorName,
    float Watts,
    HardwareType HardwareType
);

public class HardwareMonitorService : IDisposable
{
    private readonly Computer _computer;
    private bool _disposed;

    public HardwareMonitorService()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsControllerEnabled = true,
            IsStorageEnabled = true,
            IsPsuEnabled = true,
        };
        _computer.Open();
    }

    public List<PowerReading> GetPowerReadings()
    {
        var readings = new List<PowerReading>();

        foreach (var hardware in _computer.Hardware)
        {
            hardware.Update();
            foreach (var sub in hardware.SubHardware)
                sub.Update();

            CollectPower(readings, hardware.Name, hardware, hardware.HardwareType);

            foreach (var sub in hardware.SubHardware)
                CollectPower(readings, $"{hardware.Name} / {sub.Name}", sub, sub.HardwareType);
        }

        return readings;
    }

    private static void CollectPower(
        List<PowerReading> output,
        string componentName,
        IHardware hardware,
        HardwareType type)
    {
        foreach (var sensor in hardware.Sensors)
        {
            if (sensor.SensorType == SensorType.Power && sensor.Value.HasValue)
            {
                output.Add(new PowerReading(
                    componentName,
                    sensor.Name,
                    sensor.Value.Value,
                    type));
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _computer.Close();
            _disposed = true;
        }
    }
}
