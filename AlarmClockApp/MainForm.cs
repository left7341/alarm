using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace AlarmClockApp;

internal sealed class MainForm : Form
{
    private const uint FlashwAll = 0x00000003;
    private const uint FlashwTimerNoForeground = 0x0000000C;
    private const uint FlashwStop = 0;

    private readonly System.Windows.Forms.Timer _clockTimer = new() { Interval = 500 };
    private readonly NotifyIcon _notifyIcon = new();
    private readonly ContextMenuStrip _contextMenu = new();
    private readonly Icon _normalIcon = CreateAlarmIcon(alert: false);
    private readonly Icon _alertIcon = CreateAlarmIcon(alert: true);
    private readonly Label _statusLabel = new();
    private readonly Label _infoLabel = new();
    private AlarmConfiguration _configuration;
    private bool _exitRequested;
    private bool _flashStarted;
    private bool _alertIconVisible;
    private string _lastTooltip = string.Empty;

    internal MainForm()
    {
        try
        {
            _configuration = AlarmStore.Load();
        }
        catch (Exception exception)
        {
            AppLogger.Log(exception);
            MessageBox.Show(
                $"无法读取闹钟配置，程序将以空配置启动。错误日志：\n{AppLogger.LogPath}\n\n{exception.Message}",
                "任务栏闹钟 - 配置错误",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            _configuration = new AlarmConfiguration();
        }

        Text = "任务栏闹钟";
        Icon = _normalIcon;
        ShowInTaskbar = true;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(390, 210);
        ClientSize = new Size(440, 235);

        _statusLabel.Location = new Point(24, 25);
        _statusLabel.Size = new Size(390, 30);
        _statusLabel.Font = new Font(Font, FontStyle.Bold);
        _infoLabel.Location = new Point(24, 62);
        _infoLabel.Size = new Size(390, 58);

        var setupButton = new Button
        {
            Text = "设置闹钟",
            Location = new Point(24, 145),
            Size = new Size(110, 34)
        };
        setupButton.Click += (_, _) => RunSafely(OpenSetup);

        var detailsButton = new Button
        {
            Text = "查看详情",
            Location = new Point(150, 145),
            Size = new Size(110, 34)
        };
        detailsButton.Click += (_, _) => RunSafely(OpenDetails);

        var minimizeButton = new Button
        {
            Text = "最小化到任务栏",
            Location = new Point(276, 145),
            Size = new Size(135, 34)
        };
        minimizeButton.Click += (_, _) => WindowState = FormWindowState.Minimized;

        Controls.AddRange([_statusLabel, _infoLabel, setupButton, detailsButton, minimizeButton]);

        _contextMenu.Items.Add("设置闹钟", null, (_, _) => RunSafely(OpenSetup));
        _contextMenu.Items.Add("查看详情", null, (_, _) => RunSafely(OpenDetails));
        _contextMenu.Items.Add("静默", null, (_, _) => RunSafely(SilenceAlarm));
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add("退出", null, (_, _) => ExitApplication());

        _notifyIcon.Icon = _normalIcon;
        _notifyIcon.Text = "任务栏闹钟 - 未设置";
        _notifyIcon.ContextMenuStrip = _contextMenu;
        _notifyIcon.Visible = true;
        _notifyIcon.DoubleClick += (_, _) => RunSafely(OpenDetails);

        _clockTimer.Tick += (_, _) => RunSafely(OnClockTick);
        Shown += (_, _) => WindowState = FormWindowState.Minimized;
        Activated += (_, _) =>
        {
            if (_configuration.Status == AlarmStatus.Ringing && !_configuration.IsRead)
            {
                StartTaskbarFlash();
            }
        };
        FormClosing += MainFormClosing;

        RefreshPresentation();
        if (_configuration.Status == AlarmStatus.Ringing && !_configuration.IsRead)
        {
            StartAlert();
        }

        _clockTimer.Start();
    }

    private void OpenSetup()
    {
        using var setupForm = new AlarmSetupForm(_configuration.AlarmTime, _configuration.Note);
        if (setupForm.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        _configuration.AlarmTime = setupForm.AlarmTime;
        _configuration.Note = setupForm.AlarmNote;
        _configuration.Status = AlarmStatus.Armed;
        _configuration.IsRead = false;
        StopAlert();
        AlarmStore.Save(_configuration);
        RefreshPresentation();
    }

    private void OpenDetails()
    {
        _configuration.IsRead = true;
        StopAlert();
        AlarmStore.Save(_configuration);
        RefreshPresentation();

        using var detailsForm = new AlarmDetailsForm(_configuration, () => RunSafely(SilenceAlarm));
        detailsForm.ShowDialog();
    }

    private void SilenceAlarm()
    {
        if (_configuration.AlarmTime is null)
        {
            return;
        }

        _configuration.Status = AlarmStatus.Snoozed;
        StopAlert();
        AlarmStore.Save(_configuration);
        RefreshPresentation();
    }

    private void OnClockTick()
    {
        if (_configuration.Status == AlarmStatus.Armed &&
            _configuration.AlarmTime is DateTime alarmTime &&
            DateTime.Now >= alarmTime)
        {
            _configuration.Status = AlarmStatus.Ringing;
            _configuration.IsRead = false;
            AlarmStore.Save(_configuration);
            StartAlert();
            _notifyIcon.ShowBalloonTip(
                5000,
                "闹钟时间到了",
                _configuration.Note.Length == 0 ? "请打开查看详情。" : _configuration.Note,
                ToolTipIcon.Info);
        }

        if (_configuration.Status == AlarmStatus.Ringing && !_configuration.IsRead)
        {
            _alertIconVisible = !_alertIconVisible;
            _notifyIcon.Icon = _alertIconVisible ? _alertIcon : _normalIcon;
        }
        else if (_alertIconVisible)
        {
            _alertIconVisible = false;
            _notifyIcon.Icon = _normalIcon;
        }

        RefreshPresentation();
    }

    private void StartAlert()
    {
        _alertIconVisible = false;
        _notifyIcon.Icon = _normalIcon;
        StartTaskbarFlash();
        RefreshPresentation();
    }

    private void StartTaskbarFlash()
    {
        var flashInfo = new FlashWindowInfo
        {
            Size = (uint)Marshal.SizeOf<FlashWindowInfo>(),
            WindowHandle = Handle,
            Flags = FlashwAll | FlashwTimerNoForeground,
            Count = uint.MaxValue,
            Timeout = 0
        };

        FlashWindowEx(ref flashInfo);
        _flashStarted = true;
    }

    private void StopAlert()
    {
        if (_flashStarted && IsHandleCreated)
        {
            var flashInfo = new FlashWindowInfo
            {
                Size = (uint)Marshal.SizeOf<FlashWindowInfo>(),
                WindowHandle = Handle,
                Flags = FlashwStop,
                Count = 0,
                Timeout = 0
            };
            FlashWindowEx(ref flashInfo);
        }

        _flashStarted = false;
        _alertIconVisible = false;
        if (_notifyIcon.Visible)
        {
            _notifyIcon.Icon = _normalIcon;
        }
    }

    private void RefreshPresentation()
    {
        DateTime? alarmTime = _configuration.AlarmTime;
        string summary;
        string tooltip;

        if (alarmTime is null || _configuration.Status == AlarmStatus.None)
        {
            summary = "当前没有已启动的闹钟。";
            tooltip = "任务栏闹钟 - 未设置";
        }
        else if (_configuration.Status == AlarmStatus.Armed)
        {
            TimeSpan remaining = alarmTime.Value - DateTime.Now;
            string countdown = remaining <= TimeSpan.Zero
                ? "即将响铃"
                : FormatRemaining(remaining);
            summary = $"闹钟时间：{alarmTime:yyyy-MM-dd HH:mm}\r\n剩余时间：{countdown}";
            tooltip = $"闹钟 {alarmTime:HH:mm} | 剩余 {countdown}";
        }
        else if (_configuration.Status == AlarmStatus.Ringing)
        {
            summary = $"闹钟时间：{alarmTime:yyyy-MM-dd HH:mm}\r\n状态：响铃（{(_configuration.IsRead ? "已读" : "未读")}）";
            tooltip = $"闹钟响铃 {alarmTime:HH:mm}{(_configuration.IsRead ? " | 已读" : string.Empty)}";
        }
        else
        {
            summary = $"闹钟时间：{alarmTime:yyyy-MM-dd HH:mm}\r\n状态：已静默，可从托盘查看详情。";
            tooltip = $"闹钟已静默 {alarmTime:HH:mm}";
        }

        _statusLabel.Text = $"状态：{StatusText(_configuration.Status)}";
        _infoLabel.Text = summary;
        if (!string.Equals(_lastTooltip, tooltip, StringComparison.Ordinal))
        {
            _notifyIcon.Text = tooltip.Length <= 63 ? tooltip : tooltip[..63];
            _lastTooltip = tooltip;
        }
    }

    private static string FormatRemaining(TimeSpan remaining)
    {
        int totalHours = (int)remaining.TotalHours;
        return totalHours >= 24
            ? $"{remaining.Days}天 {remaining.Hours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}"
            : $"{totalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
    }

    private static string StatusText(AlarmStatus status) => status switch
    {
        AlarmStatus.Armed => "已启动",
        AlarmStatus.Ringing => "响铃",
        AlarmStatus.Snoozed => "已静默",
        _ => "未设置"
    };

    private void RunSafely(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            AppLogger.Log(exception);
            MessageBox.Show(
                this,
                $"操作失败。详细信息已写入日志：\n{AppLogger.LogPath}\n\n{exception.Message}",
                "任务栏闹钟 - 错误",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ExitApplication()
    {
        _exitRequested = true;
        _clockTimer.Stop();
        StopAlert();
        _notifyIcon.Visible = false;
        Close();
    }

    private void MainFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_exitRequested && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            WindowState = FormWindowState.Minimized;
            return;
        }

        _clockTimer.Stop();
        StopAlert();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
        _clockTimer.Dispose();
        _normalIcon.Dispose();
        _alertIcon.Dispose();
    }

    private static Icon CreateAlarmIcon(bool alert)
    {
        using var bitmap = new Bitmap(32, 32);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        using (var background = new SolidBrush(alert ? Color.Firebrick : Color.SteelBlue))
        using (var face = new SolidBrush(Color.White))
        using (var hand = new Pen(alert ? Color.Firebrick : Color.SteelBlue, 2.5f))
        using (var border = new Pen(Color.White, 1.5f))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);
            graphics.FillEllipse(background, 2, 2, 28, 28);
            graphics.FillEllipse(face, 5, 5, 22, 22);
            graphics.DrawEllipse(border, 2, 2, 28, 28);
            graphics.DrawLine(hand, 16, 16, 16, 9);
            graphics.DrawLine(hand, 16, 16, 22, 19);
            graphics.FillEllipse(background, 14, 14, 4, 4);
        }

        IntPtr iconHandle = bitmap.GetHicon();
        try
        {
            using Icon temporaryIcon = Icon.FromHandle(iconHandle);
            return (Icon)temporaryIcon.Clone();
        }
        finally
        {
            DestroyIcon(iconHandle);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FlashWindowEx(ref FlashWindowInfo info);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr iconHandle);

    [StructLayout(LayoutKind.Sequential)]
    private struct FlashWindowInfo
    {
        public uint Size;
        public IntPtr WindowHandle;
        public uint Flags;
        public uint Count;
        public uint Timeout;
    }
}
