using LibreHardwareMonitor.Hardware;

namespace Yomi.App.Services;

/// <summary>LibreHardwareMonitorLib の Computer インスタンスを管理するラッパー。</summary>
public sealed class HardwareMonitorService : IDisposable
{
    private readonly Computer _computer = new()
    {
        IsCpuEnabled = true,
        IsGpuEnabled = true,
        IsMemoryEnabled = true,
    };
    private readonly UpdateVisitor _visitor = new();
    private bool _isOpen;

    public void Open()
    {
        if (_isOpen) return;
        _computer.Open();
        _isOpen = true;
    }

    public void Update() => _computer.Accept(_visitor);

    public IReadOnlyList<IHardware> Hardware => _computer.Hardware.ToList();

    public void Dispose()
    {
        if (!_isOpen) return;
        _computer.Close();
        _isOpen = false;
    }

    private sealed class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer) => computer.Traverse(this);

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (var sub in hardware.SubHardware)
            {
                sub.Accept(this);
            }
        }

        public void VisitSensor(ISensor sensor) { }

        public void VisitParameter(IParameter parameter) { }
    }
}
