using System.Runtime.InteropServices;

namespace Lab6;

internal static class Native
{
    public const uint MEM_COMMIT = 0x1000;
    public const uint MEM_RESERVE = 0x2000;
    public const uint MEM_DECOMMIT = 0x4000;
    public const uint MEM_RELEASE = 0x8000;

    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEM_INFO
    {
        public ushort wProcessorArchitecture;
        public ushort wReserved;
        public uint dwPageSize;
        public nint lpMinimumApplicationAddress;
        public nint lpMaximumApplicationAddress;
        public nuint dwActiveProcessorMask;
        public uint dwNumberOfProcessors;
        public uint dwProcessorType;
        public uint dwAllocationGranularity;
        public ushort wProcessorLevel;
        public ushort wProcessorRevision;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORYSTATUS
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public nuint dwTotalPhys;
        public nuint dwAvailPhys;
        public nuint dwTotalPageFile;
        public nuint dwAvailPageFile;
        public nuint dwTotalVirtual;
        public nuint dwAvailVirtual;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORY_BASIC_INFORMATION
    {
        public nint BaseAddress;
        public nint AllocationBase;
        public PageProtection AllocationProtect;
        public nuint RegionSize;
        public MemoryState State;
        public PageProtection Protect;
        public MemoryType Type;
    }

    [DllImport("kernel32.dll")]
    public static extern void GetSystemInfo(out SYSTEM_INFO info);

    [DllImport("kernel32.dll")]
    public static extern void GlobalMemoryStatus(ref MEMORYSTATUS status);

    [DllImport("kernel32.dll")]
    public static extern nuint VirtualQuery(nint address, out MEMORY_BASIC_INFORMATION info, nuint length);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint VirtualAlloc(nint address, nuint size, uint allocationType, PageProtection protect);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool VirtualFree(nint address, nuint size, uint freeType);
}
