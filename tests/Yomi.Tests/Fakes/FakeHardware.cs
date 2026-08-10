using LibreHardwareMonitor.Hardware;

namespace Yomi.Tests.Fakes;

internal sealed class FakeHardware(string name, HardwareType hardwareType, params ISensor[] sensors) : IHardware
{
    public string Name { get; set; } = name;
    public Identifier Identifier { get; } = new("fake", name.Replace(" ", "_"));
    public HardwareType HardwareType { get; } = hardwareType;
    public IHardware? Parent => null;
    public IHardware[] SubHardware { get; } = [];
    public ISensor[] Sensors { get; } = sensors;
    public IDictionary<string, string> Properties { get; } = new Dictionary<string, string>();

    public event SensorEventHandler? SensorAdded;
    public event SensorEventHandler? SensorRemoved;

    public string GetReport() => string.Empty;
    public void Update() { }
    public void Accept(IVisitor visitor) { }
    public void Traverse(IVisitor visitor) { }
}
