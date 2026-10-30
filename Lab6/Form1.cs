using System;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Lab6
{
    public class Form1 : Form
    {
        const string SProc = "Processor Architecture {0}";
        const string SPageSize = "Page Size ${0:X8} bytes";
        const string SMinAddr = "Minimum Application Address ${0}";
        const string SMaxAddr = "Maximum Application Address ${0}";
        const string SNumProcs = "Number of Processors {0}";
        const string SAllocGra = "Allocation Granularity ${0:X8} bytes";
        const string SProcLevel = "Processor Level {0}";
        const string SIntel3 = "80386";
        const string SIntel4 = "80486";
        const string SIntel5 = "Pentium";
        const string SIntel6 = "Pentium Pro";
        const string SProcRev = "Processor Revision {0:X4}";

        const string SMemUse = "Memory in use {0}%";
        const string STotMem = "Total physical memory ${0:X8} bytes";
        const string SFreeMem = "Free physical memory ${0:X8} bytes";
        const string STotPage = "Total page file memory ${0:X8} bytes";
        const string SFreePage = "Free page file memory ${0:X8} bytes";
        const string STotVirt = "Total virtual memory ${0:X8} bytes";
        const string SFreeVirt = "Free virtual memory ${0:X8} bytes";

        TextBox memo;
        TextBox addressBox;

        public Form1()
        {
            Text = "Практическое занятие 6. Виртуальная память Windows";
            Width = 1000;
            Height = 700;

            memo = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                WordWrap = false,
                ScrollBars = ScrollBars.Both,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10)
            };

            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(5)
            };

            AddButton(panel, "1. GetSystemInfo", F1Click);
            AddButton(panel, "2. GlobalMemoryStatus", F2Click);

            panel.Controls.Add(new Label { Text = "Адрес (hex):", AutoSize = true, Margin = new Padding(10, 9, 0, 0) });
            addressBox = new TextBox { Width = 150, Text = "10000", Margin = new Padding(3, 6, 3, 3) };
            panel.Controls.Add(addressBox);

            AddButton(panel, "3. VirtualQuery", F3Click);
            AddButton(panel, "3. Карта памяти", MapClick);
            AddButton(panel, "VirtualAlloc / VirtualFree", AllocClick);
            AddButton(panel, "Очистить", (s, e) => memo.Clear());

            Controls.Add(memo);
            Controls.Add(panel);
        }

        void AddButton(FlowLayoutPanel panel, string text, EventHandler handler)
        {
            var button = new Button { Text = text, AutoSize = true, Height = 30 };
            button.Click += handler;
            panel.Controls.Add(button);
        }

        void Add(string line)
        {
            memo.AppendText(line + Environment.NewLine);
        }

        static string Hex(IntPtr p)
        {
            return p.ToInt64().ToString("X" + IntPtr.Size * 2);
        }

        static ulong U(UIntPtr v)
        {
            return v.ToUInt64();
        }

        static string Mb(ulong bytes)
        {
            return (bytes / 1024.0 / 1024.0).ToString("N1") + " MB";
        }

        void F1Click(object sender, EventArgs e)
        {
            NativeMethods.GetSystemInfo(out SYSTEM_INFO si);

            Add("===== GetSystemInfo =====");
            Add(string.Format(SProc, ArchitectureName(si.wProcessorArchitecture)));
            Add(string.Format(SPageSize, si.dwPageSize) + "  (" + si.dwPageSize + " bytes)");
            Add(string.Format(SMinAddr, Hex(si.lpMinimumApplicationAddress)));
            Add(string.Format(SMaxAddr, Hex(si.lpMaximumApplicationAddress)));
            Add("Active Processor Mask $" + U(si.dwActiveProcessorMask).ToString("X"));
            Add(string.Format(SNumProcs, si.dwNumberOfProcessors));
            Add("Processor Type " + si.dwProcessorType);
            Add(string.Format(SAllocGra, si.dwAllocationGranularity) + "  (" + si.dwAllocationGranularity / 1024 + " KB)");

            switch (si.wProcessorLevel)
            {
                case 3: Add(string.Format(SProcLevel, SIntel3)); break;
                case 4: Add(string.Format(SProcLevel, SIntel4)); break;
                case 5: Add(string.Format(SProcLevel, SIntel5)); break;
                case 6: Add(string.Format(SProcLevel, SIntel6)); break;
                default: Add(string.Format(SProcLevel, si.wProcessorLevel)); break;
            }

            Add(string.Format(SProcRev, si.wProcessorRevision));
            Add("");

            addressBox.Text = Hex(si.lpMinimumApplicationAddress);
        }

        static string ArchitectureName(ushort arch)
        {
            switch (arch)
            {
                case 0: return "Intel (x86)";
                case 5: return "ARM";
                case 6: return "Intel Itanium (IA64)";
                case 9: return "AMD64 (x64)";
                case 12: return "ARM64";
                default: return "Unknown (" + arch + ")";
            }
        }

        void F2Click(object sender, EventArgs e)
        {
            var ms = new MEMORYSTATUS();
            ms.dwLength = (uint)Marshal.SizeOf(ms);
            NativeMethods.GlobalMemoryStatus(ref ms);

            Add("===== GlobalMemoryStatus =====");
            Add(string.Format(SMemUse, ms.dwMemoryLoad));
            Add(string.Format(STotMem, U(ms.dwTotalPhys)) + "  (" + Mb(U(ms.dwTotalPhys)) + ")");
            Add(string.Format(SFreeMem, U(ms.dwAvailPhys)) + "  (" + Mb(U(ms.dwAvailPhys)) + ")");
            Add(string.Format(STotPage, U(ms.dwTotalPageFile)) + "  (" + Mb(U(ms.dwTotalPageFile)) + ")");
            Add(string.Format(SFreePage, U(ms.dwAvailPageFile)) + "  (" + Mb(U(ms.dwAvailPageFile)) + ")");
            Add(string.Format(STotVirt, U(ms.dwTotalVirtual)) + "  (" + Mb(U(ms.dwTotalVirtual)) + ")");
            Add(string.Format(SFreeVirt, U(ms.dwAvailVirtual)) + "  (" + Mb(U(ms.dwAvailVirtual)) + ")");
            Add("");
        }

        void F3Click(object sender, EventArgs e)
        {
            string text = addressBox.Text.Trim().TrimStart('$');
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(2);

            if (!long.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long address))
            {
                MessageBox.Show("Введите адрес в шестнадцатеричном виде, например 7FFE0000");
                return;
            }

            Add("===== VirtualQuery($" + address.ToString("X") + ") =====");
            ShowRegion(new IntPtr(address));
            Add("");
        }

        bool Query(IntPtr address, out MEMORY_BASIC_INFORMATION mbi)
        {
            var size = (UIntPtr)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION));
            return NativeMethods.VirtualQuery(address, out mbi, size) != UIntPtr.Zero;
        }

        void ShowRegion(IntPtr address)
        {
            if (!Query(address, out MEMORY_BASIC_INFORMATION mbi))
            {
                Add("VirtualQuery вернула 0: адрес вне пользовательского адресного пространства");
                return;
            }

            Add("BaseAddress       $" + Hex(mbi.BaseAddress));
            Add("AllocationBase    $" + Hex(mbi.AllocationBase));
            Add("AllocationProtect " + ProtectName(mbi.AllocationProtect));
            Add("RegionSize        $" + U(mbi.RegionSize).ToString("X8") + "  (" + U(mbi.RegionSize) / 1024 + " KB)");
            Add("State             " + StateName(mbi.State));
            Add("Protect           " + ProtectName(mbi.Protect));
            Add("Type              " + TypeName(mbi.Type));
        }

        void MapClick(object sender, EventArgs e)
        {
            NativeMethods.GetSystemInfo(out SYSTEM_INFO si);
            long address = si.lpMinimumApplicationAddress.ToInt64();
            long max = si.lpMaximumApplicationAddress.ToInt64();
            int digits = IntPtr.Size * 2;

            var sb = new StringBuilder();
            sb.AppendLine("===== Карта виртуального адресного пространства =====");
            sb.AppendLine(string.Format("{0,-" + (digits + 2) + "} {1,-" + (digits + 2) + "} {2,14}  {3,-12} {4,-26} {5}",
                "Base", "AllocBase", "Size (KB)", "State", "Protect", "Type"));

            int count = 0;
            long free = 0, reserved = 0, committed = 0;

            while (address < max)
            {
                if (!Query(new IntPtr(address), out MEMORY_BASIC_INFORMATION mbi))
                    break;

                long size = (long)U(mbi.RegionSize);
                bool isFree = mbi.State == NativeMethods.MEM_FREE;

                sb.AppendLine(string.Format("{0,-" + (digits + 2) + "} {1,-" + (digits + 2) + "} {2,14:N0}  {3,-12} {4,-26} {5}",
                    "$" + Hex(mbi.BaseAddress),
                    isFree ? "" : "$" + Hex(mbi.AllocationBase),
                    size / 1024,
                    StateName(mbi.State),
                    isFree ? "" : ProtectName(mbi.Protect),
                    isFree ? "" : TypeName(mbi.Type)));

                if (mbi.State == NativeMethods.MEM_FREE) free += size;
                if (mbi.State == NativeMethods.MEM_RESERVE) reserved += size;
                if (mbi.State == NativeMethods.MEM_COMMIT) committed += size;

                count++;
                address = mbi.BaseAddress.ToInt64() + size;
            }

            sb.AppendLine();
            sb.AppendLine("Регионов: " + count);
            sb.AppendLine("MEM_FREE:    " + Mb((ulong)free));
            sb.AppendLine("MEM_RESERVE: " + Mb((ulong)reserved));
            sb.AppendLine("MEM_COMMIT:  " + Mb((ulong)committed));
            sb.AppendLine();

            memo.AppendText(sb.ToString());
        }

        void AllocClick(object sender, EventArgs e)
        {
            NativeMethods.GetSystemInfo(out SYSTEM_INFO si);
            var regionSize = (UIntPtr)si.dwAllocationGranularity;
            var pageSize = (UIntPtr)si.dwPageSize;

            Add("===== VirtualAlloc / VirtualFree =====");

            IntPtr p = NativeMethods.VirtualAlloc(IntPtr.Zero, regionSize, NativeMethods.MEM_RESERVE, NativeMethods.PAGE_NOACCESS);
            if (p == IntPtr.Zero)
            {
                Add("Ошибка VirtualAlloc(MEM_RESERVE): " + Marshal.GetLastWin32Error());
                return;
            }

            Add("--- 1) Регион зарезервирован: VirtualAlloc(NULL, " + U(regionSize) + ", MEM_RESERVE, PAGE_NOACCESS)");
            ShowRegion(p);

            NativeMethods.VirtualAlloc(p, pageSize, NativeMethods.MEM_COMMIT, NativeMethods.PAGE_READWRITE);
            Add("--- 2) Первой странице передана физическая память: VirtualAlloc(p, " + U(pageSize) + ", MEM_COMMIT, PAGE_READWRITE)");
            ShowRegion(p);

            Marshal.WriteInt32(p, 12345);
            Add("--- 3) Записали в память число 12345, прочитали: " + Marshal.ReadInt32(p));

            NativeMethods.VirtualFree(p, pageSize, NativeMethods.MEM_DECOMMIT);
            Add("--- 4) Физическая память возвращена: VirtualFree(p, " + U(pageSize) + ", MEM_DECOMMIT)");
            ShowRegion(p);

            NativeMethods.VirtualFree(p, UIntPtr.Zero, NativeMethods.MEM_RELEASE);
            Add("--- 5) Регион освобождён: VirtualFree(p, 0, MEM_RELEASE)");
            ShowRegion(p);
            Add("");

            addressBox.Text = Hex(p);
        }

        static string StateName(uint state)
        {
            switch (state)
            {
                case NativeMethods.MEM_COMMIT: return "MEM_COMMIT";
                case NativeMethods.MEM_RESERVE: return "MEM_RESERVE";
                case NativeMethods.MEM_FREE: return "MEM_FREE";
                default: return "$" + state.ToString("X");
            }
        }

        static string TypeName(uint type)
        {
            switch (type)
            {
                case NativeMethods.MEM_IMAGE: return "MEM_IMAGE";
                case NativeMethods.MEM_MAPPED: return "MEM_MAPPED";
                case NativeMethods.MEM_PRIVATE: return "MEM_PRIVATE";
                case 0: return "-";
                default: return "$" + type.ToString("X");
            }
        }

        static string ProtectName(uint protect)
        {
            if (protect == 0)
                return "-";

            string name;
            switch (protect & 0xFF)
            {
                case NativeMethods.PAGE_NOACCESS: name = "PAGE_NOACCESS"; break;
                case NativeMethods.PAGE_READONLY: name = "PAGE_READONLY"; break;
                case NativeMethods.PAGE_READWRITE: name = "PAGE_READWRITE"; break;
                case NativeMethods.PAGE_WRITECOPY: name = "PAGE_WRITECOPY"; break;
                case NativeMethods.PAGE_EXECUTE: name = "PAGE_EXECUTE"; break;
                case NativeMethods.PAGE_EXECUTE_READ: name = "PAGE_EXECUTE_READ"; break;
                case NativeMethods.PAGE_EXECUTE_READWRITE: name = "PAGE_EXECUTE_READWRITE"; break;
                case NativeMethods.PAGE_EXECUTE_WRITECOPY: name = "PAGE_EXECUTE_WRITECOPY"; break;
                default: name = "$" + (protect & 0xFF).ToString("X"); break;
            }

            if ((protect & NativeMethods.PAGE_GUARD) != 0) name += "|GUARD";
            if ((protect & NativeMethods.PAGE_NOCACHE) != 0) name += "|NOCACHE";
            if ((protect & NativeMethods.PAGE_WRITECOMBINE) != 0) name += "|WRITECOMBINE";
            return name;
        }
    }
}
