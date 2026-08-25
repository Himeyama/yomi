using System.Runtime.InteropServices;

namespace Yomi.App.Services.Providers;

/// <summary>DXGI から取得したディスプレイアダプター1件分の情報。</summary>
public readonly record struct DxgiAdapterInfo(string Name, long Luid, double VramTotalGiB);

/// <summary>
/// DXGI (IDXGIFactory1::EnumAdapters1) を使い、ディスプレイアダプターの
/// 名前・LUID・専用VRAM総量を取得する。
///
/// レジストリの qwMemorySize と異なり、DXGI は AdapterLuid を提供するため、
/// "GPU Adapter Memory" パフォーマンスカウンターのインスタンス名(luid_...)と
/// 正確に同一アダプターを紐付けられる。
/// </summary>
public static class DxgiAdapterInfoProvider
{
    public static IReadOnlyList<DxgiAdapterInfo> QueryAdapters()
    {
        var adapters = new List<DxgiAdapterInfo>();

        if (NativeMethods.CreateDXGIFactory1(NativeMethods.IID_IDXGIFactory1, out var factoryPtr) != 0
            || factoryPtr == IntPtr.Zero)
        {
            return adapters;
        }

        try
        {
            uint index = 0;
            while (NativeMethods.EnumAdapters1(factoryPtr, index, out var adapterPtr) == 0)
            {
                index++;
                if (adapterPtr == IntPtr.Zero) continue;

                try
                {
                    if (NativeMethods.GetDesc1(adapterPtr, out var desc) == 0)
                    {
                        // ソフトウェアアダプター(Microsoft Basic Render Driver等)は除外する。
                        const uint DXGI_ADAPTER_FLAG_SOFTWARE = 2;
                        if ((desc.Flags & DXGI_ADAPTER_FLAG_SOFTWARE) == 0 && desc.DedicatedVideoMemory > 0)
                        {
                            var luid = ((long)desc.AdapterLuidHighPart << 32) | (uint)desc.AdapterLuidLowPart;
                            adapters.Add(new DxgiAdapterInfo(
                                desc.Description,
                                luid,
                                desc.DedicatedVideoMemory / (1024.0 * 1024.0 * 1024.0)));
                        }
                    }
                }
                finally
                {
                    Marshal.Release(adapterPtr);
                }
            }
        }
        finally
        {
            Marshal.Release(factoryPtr);
        }

        return adapters;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DXGI_ADAPTER_DESC1
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Description;
        public uint VendorId;
        public uint DeviceId;
        public uint SubSysId;
        public uint Revision;
        public nuint DedicatedVideoMemory;
        public nuint DedicatedSystemMemory;
        public nuint SharedSystemMemory;
        public int AdapterLuidLowPart;
        public int AdapterLuidHighPart;
        public uint Flags;
    }

    private static class NativeMethods
    {
        public static readonly Guid IID_IDXGIFactory1 = new("770aae78-f26f-4dba-a829-253c83d1b387");

        [DllImport("dxgi.dll", ExactSpelling = true)]
        public static extern int CreateDXGIFactory1(in Guid riid, out IntPtr ppFactory);

        // IDXGIFactory1::EnumAdapters1 は vtable index 12 (IUnknown 3 + IDXGIObject 4 + IDXGIFactory 5 = 12)。
        public static int EnumAdapters1(IntPtr factory, uint index, out IntPtr adapter)
        {
            var vtable = Marshal.ReadIntPtr(factory);
            var fn = Marshal.GetDelegateForFunctionPointer<EnumAdapters1Delegate>(Marshal.ReadIntPtr(vtable, 12 * IntPtr.Size));
            return fn(factory, index, out adapter);
        }

        // IDXGIAdapter1::GetDesc1 は vtable index 10 (IUnknown 3 + IDXGIObject 4 + IDXGIAdapter 3 = 10)。
        public static int GetDesc1(IntPtr adapter, out DXGI_ADAPTER_DESC1 desc)
        {
            var vtable = Marshal.ReadIntPtr(adapter);
            var fn = Marshal.GetDelegateForFunctionPointer<GetDesc1Delegate>(Marshal.ReadIntPtr(vtable, 10 * IntPtr.Size));
            return fn(adapter, out desc);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int EnumAdapters1Delegate(IntPtr factory, uint index, out IntPtr adapter);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetDesc1Delegate(IntPtr adapter, out DXGI_ADAPTER_DESC1 desc);
    }
}
