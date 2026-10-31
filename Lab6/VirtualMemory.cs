using System.Runtime.InteropServices;

namespace Lab6;

public static class VirtualMemory
{
    public static SystemInfo GetSystemInfo()
    {
        Native.GetSystemInfo(out var si);

        return new SystemInfo(
            ArchitectureName(si.wProcessorArchitecture),
            si.dwPageSize,
            (ulong)si.lpMinimumApplicationAddress,
            (ulong)si.lpMaximumApplicationAddress,
            si.dwActiveProcessorMask,
            si.dwNumberOfProcessors,
            si.dwProcessorType,
            si.dwAllocationGranularity,
            si.wProcessorLevel,
            si.wProcessorRevision);
    }

    public static MemoryStatus GetMemoryStatus()
    {
        var ms = new Native.MEMORYSTATUS { dwLength = (uint)Marshal.SizeOf<Native.MEMORYSTATUS>() };
        Native.GlobalMemoryStatus(ref ms);

        return new MemoryStatus(
            ms.dwMemoryLoad,
            ms.dwTotalPhys,
            ms.dwAvailPhys,
            ms.dwTotalPageFile,
            ms.dwAvailPageFile,
            ms.dwTotalVirtual,
            ms.dwAvailVirtual);
    }

    public static MemoryRegion? Query(ulong address)
    {
        var size = (nuint)Marshal.SizeOf<Native.MEMORY_BASIC_INFORMATION>();
        if (Native.VirtualQuery((nint)address, out var mbi, size) == 0)
            return null;

        return new MemoryRegion(
            (ulong)mbi.BaseAddress,
            (ulong)mbi.AllocationBase,
            mbi.AllocationProtect,
            mbi.RegionSize,
            mbi.State,
            mbi.Protect,
            mbi.Type);
    }

    public static IEnumerable<MemoryRegion> GetRegions()
    {
        var info = GetSystemInfo();
        var address = info.MinAddress;

        while (address < info.MaxAddress && Query(address) is { } region)
        {
            yield return region;
            address = region.EndAddress;
        }
    }

    static string ArchitectureName(ushort code) => code switch
    {
        0 => "x86 (Intel)",
        5 => "ARM",
        6 => "IA64 (Itanium)",
        9 => "x64 (AMD64)",
        12 => "ARM64",
        _ => $"Неизвестно ({code})"
    };
}
