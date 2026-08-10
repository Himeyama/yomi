using LibreHardwareMonitor.Hardware;

namespace Yomi.Tests.Fakes;

internal sealed class FakeSensor(string name, SensorType sensorType, float? value) : ISensor
{
    public IControl? Control => null;
    public IHardware Hardware => null!;
    public Identifier Identifier { get; } = new("fake", name.Replace(" ", "_"));
    public int Index => 0;
    public bool IsDefaultHidden => false;
    public string Name { get; set; } = name;
    public IReadOnlyList<IParameter> Parameters { get; } = [];
    public SensorType SensorType { get; } = sensorType;
    public float? Value { get; } = value;
    public float? Min => null;
    public float? Max => null;
    public IEnumerable<SensorValue> Values => [];
    public TimeSpan ValuesTimeWindow { get; set; }

    public void ResetMin() { }
    public void ResetMax() { }
    public void ClearValues() { }
    public void Accept(IVisitor visitor) { }
    public void Traverse(IVisitor visitor) { }
}
