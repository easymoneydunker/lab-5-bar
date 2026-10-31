using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Lab6;

public class Form1 : Form
{
    readonly ListView systemList = CreatePairList(320, 420);
    readonly ListView memoryList = CreatePairList(320, 420);
    readonly ListView regionList = CreatePairList(150, 260);
    readonly ComboBox stateFilter = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
    readonly TextBox addressBox = new() { Width = 170 };
    readonly Label mapSummary = new() { AutoSize = true, Margin = new Padding(10, 8, 0, 0) };
    readonly CheckBox autoRefresh = new() { Text = "Обновлять каждую секунду", AutoSize = true, Margin = new Padding(10, 6, 0, 0) };
    readonly System.Windows.Forms.Timer timer = new() { Interval = 1000 };

    readonly DataGridView mapGrid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
        Font = new Font("Consolas", 9)
    };

    readonly TextBox allocLog = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Font = new Font("Consolas", 10)
    };

    List<MemoryRegion> regions = new();
    List<(ulong Start, ulong End, string Name)> modules = new();

    public Form1()
    {
        Text = "Практическое занятие 6. Виртуальная память Windows";
        Size = new Size(1150, 720);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(CreateSystemTab());
        tabs.TabPages.Add(CreateMemoryTab());
        tabs.TabPages.Add(CreateMapTab());
        tabs.TabPages.Add(CreateAllocTab());
        Controls.Add(tabs);

        timer.Tick += (_, _) => ShowMemoryStatus();
        autoRefresh.CheckedChanged += (_, _) => timer.Enabled = autoRefresh.Checked;
        stateFilter.SelectedIndexChanged += (_, _) => ShowMap();
        mapGrid.CellDoubleClick += MapGridDoubleClick;

        Load += (_, _) =>
        {
            ShowSystemInfo();
            ShowMemoryStatus();
            BuildMap();
        };
    }

    TabPage CreateSystemTab() =>
        CreateTab("1. GetSystemInfo", systemList,
            CreateButton("Обновить", ShowSystemInfo));

    TabPage CreateMemoryTab() =>
        CreateTab("2. GlobalMemoryStatus", memoryList,
            CreateButton("Обновить", ShowMemoryStatus),
            autoRefresh);

    TabPage CreateMapTab()
    {
        stateFilter.Items.Add("Все");
        stateFilter.Items.AddRange(Enum.GetValues<MemoryState>().Cast<object>().ToArray());
        stateFilter.SelectedIndex = 0;

        regionList.Dock = DockStyle.Right;
        regionList.Width = 420;

        var content = new Panel { Dock = DockStyle.Fill };
        content.Controls.Add(mapGrid);
        content.Controls.Add(regionList);

        return CreateTab("3. VirtualQuery", content,
            CreateButton("Построить карту", BuildMap),
            CreateLabel("Состояние:"), stateFilter,
            CreateLabel("Адрес:"), addressBox,
            CreateButton("VirtualQuery", QueryAddress),
            mapSummary);
    }

    TabPage CreateAllocTab() =>
        CreateTab("VirtualAlloc / VirtualFree", allocLog,
            CreateButton("Выполнить", RunAllocDemo));

    void ShowSystemInfo()
    {
        var si = VirtualMemory.GetSystemInfo();

        Fill(systemList,
            ("Архитектура процессора", si.Architecture),
            ("Размер страницы", $"{si.PageSize:N0} байт (0x{si.PageSize:X8})"),
            ("Минимальный адрес приложения", si.MinAddress.Hex()),
            ("Максимальный адрес приложения", si.MaxAddress.Hex()),
            ("Маска активных процессоров", $"0x{si.ActiveProcessorMask:X}"),
            ("Количество процессоров", si.ProcessorCount.ToString()),
            ("Тип процессора", si.ProcessorType.ToString()),
            ("Гранулярность выделения памяти", $"{si.AllocationGranularity:N0} байт (0x{si.AllocationGranularity:X8})"),
            ("Уровень процессора", si.ProcessorLevelName),
            ("Ревизия процессора", $"{si.ProcessorRevision:X4}"),
            ("", ""),
            (".NET: Environment.SystemPageSize", $"{Environment.SystemPageSize:N0} байт"),
            (".NET: Environment.ProcessorCount", Environment.ProcessorCount.ToString()),
            (".NET: RuntimeInformation.OSArchitecture", RuntimeInformation.OSArchitecture.ToString()),
            (".NET: Environment.Is64BitProcess", Environment.Is64BitProcess ? "Да" : "Нет"));

        addressBox.Text = si.MinAddress.Hex();
    }

    void ShowMemoryStatus()
    {
        var ms = VirtualMemory.GetMemoryStatus();
        using var process = Process.GetCurrentProcess();

        Fill(memoryList,
            ("Загрузка памяти", $"{ms.MemoryLoad}%"),
            ("Всего физической памяти", Describe(ms.TotalPhys)),
            ("Свободно физической памяти", Describe(ms.AvailPhys)),
            ("Всего в файле подкачки", Describe(ms.TotalPageFile)),
            ("Свободно в файле подкачки", Describe(ms.AvailPageFile)),
            ("Всего виртуальной памяти процесса", Describe(ms.TotalVirtual)),
            ("Свободно виртуальной памяти процесса", Describe(ms.AvailVirtual)),
            ("", ""),
            (".NET: Process.WorkingSet64", Describe((ulong)process.WorkingSet64)),
            (".NET: Process.PrivateMemorySize64", Describe((ulong)process.PrivateMemorySize64)),
            (".NET: Process.VirtualMemorySize64", Describe((ulong)process.VirtualMemorySize64)),
            (".NET: GC.GetTotalMemory", Describe((ulong)GC.GetTotalMemory(false))),
            ("Время замера", DateTime.Now.ToLongTimeString()));
    }

    void BuildMap()
    {
        using var process = Process.GetCurrentProcess();
        modules = process.Modules
            .Cast<ProcessModule>()
            .Select(m => ((ulong)m.BaseAddress, (ulong)m.BaseAddress + (ulong)m.ModuleMemorySize, m.ModuleName))
            .ToList();

        regions = VirtualMemory.GetRegions().ToList();
        ShowMap();
    }

    void ShowMap()
    {
        var filtered = stateFilter.SelectedItem is MemoryState state
            ? regions.Where(r => r.State == state)
            : regions;

        mapGrid.DataSource = filtered.Select(r => new
        {
            Адрес = r.BaseAddress.Hex(),
            Регион = r.IsFree ? "" : r.AllocationBase.Hex(),
            Размер = r.Size.Bytes(),
            Состояние = r.State,
            Защита = r.IsFree ? "" : r.Protect.ToString(),
            Тип = r.IsFree ? "" : r.Type.ToString(),
            Модуль = ModuleOf(r)
        }).ToList();

        mapSummary.Text = string.Join("    ", regions
            .GroupBy(r => r.State)
            .Select(g => $"{g.Key}: {g.Count()} шт., {g.Aggregate(0UL, (sum, r) => sum + r.Size).Bytes()}"));
    }

    string ModuleOf(MemoryRegion region) =>
        region.Type == MemoryType.Image
            ? modules.FirstOrDefault(m => region.AllocationBase >= m.Start && region.AllocationBase < m.End).Name ?? ""
            : "";

    void MapGridDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0)
            return;

        addressBox.Text = mapGrid.Rows[e.RowIndex].Cells[0].Value?.ToString();
        QueryAddress();
    }

    void QueryAddress()
    {
        var text = addressBox.Text.Trim().TrimStart('$').Replace("0x", "", StringComparison.OrdinalIgnoreCase);

        if (!ulong.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var address))
        {
            MessageBox.Show("Введите адрес в шестнадцатеричном виде, например 0x7FFE0000");
            return;
        }

        if (VirtualMemory.Query(address) is not { } r)
        {
            Fill(regionList, ("Результат", "VirtualQuery вернула 0"));
            return;
        }

        Fill(regionList,
            ("Запрошенный адрес", address.Hex()),
            ("BaseAddress", r.BaseAddress.Hex()),
            ("AllocationBase", r.IsFree ? "-" : r.AllocationBase.Hex()),
            ("AllocationProtect", r.IsFree ? "-" : r.AllocationProtect.ToString()),
            ("RegionSize", Describe(r.Size)),
            ("State", r.State.ToString()),
            ("Protect", r.IsFree ? "-" : r.Protect.ToString()),
            ("Type", r.IsFree ? "-" : r.Type.ToString()),
            ("Модуль", ModuleOf(r)));
    }

    void RunAllocDemo()
    {
        allocLog.Clear();
        var si = VirtualMemory.GetSystemInfo();
        ulong address;

        using (var region = VirtualRegion.Reserve(si.AllocationGranularity))
        {
            address = region.Address;
            Log($"1) Reserve: VirtualAlloc(NULL, {si.AllocationGranularity}, MEM_RESERVE, PAGE_NOACCESS)", region.Info);

            region.Commit(si.PageSize);
            Log($"2) Commit: VirtualAlloc({address.Hex()}, {si.PageSize}, MEM_COMMIT, PAGE_READWRITE)", region.Info);

            region.WriteInt32(12345);
            allocLog.AppendText($"3) Записали в память 12345, прочитали: {region.ReadInt32()}{Environment.NewLine}{Environment.NewLine}");

            region.Decommit(si.PageSize);
            Log($"4) Decommit: VirtualFree({address.Hex()}, {si.PageSize}, MEM_DECOMMIT)", region.Info);
        }

        Log($"5) Dispose: VirtualFree({address.Hex()}, 0, MEM_RELEASE)", VirtualMemory.Query(address));
        addressBox.Text = address.Hex();
    }

    void Log(string step, MemoryRegion? region) =>
        allocLog.AppendText($"{step}{Environment.NewLine}   {region}{Environment.NewLine}{Environment.NewLine}");

    static string Describe(ulong bytes) => $"{bytes.Bytes()}  ({bytes:N0} байт)";

    static void Fill(ListView list, params (string Name, string Value)[] rows)
    {
        list.BeginUpdate();
        list.Items.Clear();
        foreach (var (name, value) in rows)
            list.Items.Add(new ListViewItem(new[] { name, value }));
        list.EndUpdate();
    }

    static ListView CreatePairList(int nameWidth, int valueWidth)
    {
        var list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Font = new Font("Segoe UI", 10)
        };
        list.Columns.Add("Параметр", nameWidth);
        list.Columns.Add("Значение", valueWidth);
        return list;
    }

    static TabPage CreateTab(string title, Control content, params Control[] toolbar)
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(4) };
        panel.Controls.AddRange(toolbar);

        var page = new TabPage(title);
        page.Controls.Add(content);
        page.Controls.Add(panel);
        return page;
    }

    static Button CreateButton(string text, Action action)
    {
        var button = new Button { Text = text, AutoSize = true };
        button.Click += (_, _) => action();
        return button;
    }

    static Label CreateLabel(string text) =>
        new() { Text = text, AutoSize = true, Margin = new Padding(10, 8, 0, 0) };
}
