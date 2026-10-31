namespace Lab6;

public enum MemoryState : uint
{
    Commit = 0x1000,
    Reserve = 0x2000,
    Free = 0x10000
}

public enum MemoryType : uint
{
    None = 0,
    Private = 0x20000,
    Mapped = 0x40000,
    Image = 0x1000000
}

[Flags]
public enum PageProtection : uint
{
    None = 0,
    NoAccess = 0x01,
    ReadOnly = 0x02,
    ReadWrite = 0x04,
    WriteCopy = 0x08,
    Execute = 0x10,
    ExecuteRead = 0x20,
    ExecuteReadWrite = 0x40,
    ExecuteWriteCopy = 0x80,
    Guard = 0x100,
    NoCache = 0x200,
    WriteCombine = 0x400
}

public record SystemInfo(
    string Architecture,
    uint PageSize,
    ulong MinAddress,
    ulong MaxAddress,
    ulong ActiveProcessorMask,
    uint ProcessorCount,
    uint ProcessorType,
    uint AllocationGranularity,
    ushort ProcessorLevel,
    ushort ProcessorRevision)
{
    public string ProcessorLevelName => ProcessorLevel switch
    {
        3 => "80386",
        4 => "80486",
        5 => "Pentium",
        6 => "Pentium Pro",
        _ => ProcessorLevel.ToString()
    };
}

public record MemoryStatus(
    uint MemoryLoad,
    ulong TotalPhys,
    ulong AvailPhys,
    ulong TotalPageFile,
    ulong AvailPageFile,
    ulong TotalVirtual,
    ulong AvailVirtual);

public record MemoryRegion(
    ulong BaseAddress,
    ulong AllocationBase,
    PageProtection AllocationProtect,
    ulong Size,
    MemoryState State,
    PageProtection Protect,
    MemoryType Type)
{
    public ulong EndAddress => BaseAddress + Size;
    public bool IsFree => State == MemoryState.Free;

    public override string ToString() => IsFree
        ? $"{BaseAddress.Hex()}, {Size.Bytes()}, {State}"
        : $"{BaseAddress.Hex()}, {Size.Bytes()}, {State}, {Protect}, {Type} (регион {AllocationBase.Hex()}, AllocationProtect = {AllocationProtect})";
}

public static class Formatting
{
    public static string Hex(this ulong value) => "0x" + value.ToString("X" + IntPtr.Size * 2);

    public static string Bytes(this ulong value) => value switch
    {
        >= 1UL << 40 => $"{value / (double)(1UL << 40):N1} ТБ",
        >= 1UL << 30 => $"{value / (double)(1UL << 30):N1} ГБ",
        >= 1UL << 20 => $"{value / (double)(1UL << 20):N1} МБ",
        >= 1UL << 10 => $"{value / (double)(1UL << 10):N1} КБ",
        _ => $"{value} Б"
    };
}
