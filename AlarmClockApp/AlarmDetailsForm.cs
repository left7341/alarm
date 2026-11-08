namespace AlarmClockApp;

internal sealed class AlarmDetailsForm : Form
{
    private readonly Label _statusLabel = new();

    internal AlarmDetailsForm(AlarmConfiguration configuration, Action silenceAlarm)
    {
        Text = "闹钟详情";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(390, 220);

        var timeLabel = new Label
        {
            AutoSize = true,
            Location = new Point(20, 20),
            Text = $"闹钟时间：{configuration.AlarmTime?.ToString("yyyy-MM-dd HH:mm") ?? "未设置"}"
        };
        var noteTitle = new Label
        {
            AutoSize = true,
            Location = new Point(20, 58),
            Text = "备注："
        };
        var noteLabel = new Label
        {
            Location = new Point(75, 58),
            Size = new Size(290, 65),
            Text = string.IsNullOrWhiteSpace(configuration.Note) ? "（无）" : configuration.Note
        };
        _statusLabel.AutoSize = true;
        _statusLabel.Location = new Point(20, 135);
        UpdateStatus(configuration);

        var silenceButton = new Button
        {
            Text = "静默",
            Location = new Point(185, 175),
            Size = new Size(80, 30)
        };
        silenceButton.Click += (_, _) =>
        {
            silenceAlarm();
            _statusLabel.Text = "状态：已静默（仍可从托盘查看详情）";
        };

        var closeButton = new Button
        {
            Text = "关闭",
            Location = new Point(285, 175),
            Size = new Size(80, 30),
            DialogResult = DialogResult.Cancel
        };

        Controls.AddRange([timeLabel, noteTitle, noteLabel, _statusLabel, silenceButton, closeButton]);
        CancelButton = closeButton;
    }

    private void UpdateStatus(AlarmConfiguration configuration)
    {
        string status = configuration.Status switch
        {
            AlarmStatus.Armed => "已启动",
            AlarmStatus.Ringing => configuration.IsRead ? "响铃（已读）" : "响铃",
            AlarmStatus.Snoozed => "已静默",
            _ => "未设置"
        };
        _statusLabel.Text = $"状态：{status}";
    }
}
