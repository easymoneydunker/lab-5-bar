using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Lab6;

public sealed class VirtualRegion : IDisposable
{
    bool released;

    public ulong Address { get; }
    public ulong Size { get; }
    public MemoryRegion Info => VirtualMemory.Query(Address)!;

    VirtualRegion(ulong address, ulong size)
    {
        Address = address;
        Size = size;
    }

    public static VirtualRegion Reserve(ulong size)
    {
        var address = Native.VirtualAlloc(0, (nuint)size, Native.MEM_RESERVE, PageProtection.NoAccess);
        if (address == 0)
            throw new Win32Exception();

        return new VirtualRegion((ulong)address, size);
    }

    public void Commit(ulong size, PageProtection protection = PageProtection.ReadWrite)
    {
        if (Native.VirtualAlloc((nint)Address, (nuint)size, Native.MEM_COMMIT, protection) == 0)
            throw new Win32Exception();
    }

    public void Decommit(ulong size)
    {
        if (!Native.VirtualFree((nint)Address, (nuint)size, Native.MEM_DECOMMIT))
            throw new Win32Exception();
    }

    public void WriteInt32(int value) => Marshal.WriteInt32((nint)Address, value);

    public int ReadInt32() => Marshal.ReadInt32((nint)Address);

    public void Dispose()
    {
        if (released)
            return;

        Native.VirtualFree((nint)Address, 0, Native.MEM_RELEASE);
        released = true;
    }
}
