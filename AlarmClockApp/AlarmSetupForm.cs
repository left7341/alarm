namespace AlarmClockApp;

internal sealed class AlarmSetupForm : Form
{
    private readonly DateTimePicker _alarmTimePicker = new();
    private readonly TextBox _noteTextBox = new();

    internal AlarmSetupForm(DateTime? currentAlarmTime, string currentNote)
    {
        Text = "设置闹钟";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(380, 190);

        var timeLabel = new Label
        {
            AutoSize = true,
            Location = new Point(18, 24),
            Text = "闹钟时间："
        };
        _alarmTimePicker.Format = DateTimePickerFormat.Custom;
        _alarmTimePicker.CustomFormat = "yyyy-MM-dd HH:mm";
        _alarmTimePicker.ShowUpDown = false;
        _alarmTimePicker.Value = ClampPickerValue(currentAlarmTime ?? DateTime.Now.AddMinutes(5));
        _alarmTimePicker.Location = new Point(105, 19);
        _alarmTimePicker.Size = new Size(255, 27);

        var noteLabel = new Label
        {
            AutoSize = true,
            Location = new Point(18, 68),
            Text = "备注："
        };
        _noteTextBox.Location = new Point(105, 64);
        _noteTextBox.Size = new Size(255, 58);
        _noteTextBox.Multiline = true;
        _noteTextBox.MaxLength = 500;
        _noteTextBox.Text = currentNote;

        var saveButton = new Button
        {
            Text = "保存并启动",
            Location = new Point(165, 143),
            Size = new Size(105, 30)
        };
        saveButton.Click += SaveButtonClick;

        var cancelButton = new Button
        {
            Text = "取消",
            Location = new Point(280, 143),
            Size = new Size(80, 30),
            DialogResult = DialogResult.Cancel
        };

        Controls.AddRange([timeLabel, _alarmTimePicker, noteLabel, _noteTextBox, saveButton, cancelButton]);
        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    internal DateTime AlarmTime => new(
        _alarmTimePicker.Value.Year,
        _alarmTimePicker.Value.Month,
        _alarmTimePicker.Value.Day,
        _alarmTimePicker.Value.Hour,
        _alarmTimePicker.Value.Minute,
        0);

    internal string AlarmNote => _noteTextBox.Text.Trim();

    private static DateTime ClampPickerValue(DateTime value)
    {
        if (value < DateTimePicker.MinimumDateTime)
        {
            return DateTimePicker.MinimumDateTime;
        }

        return value > DateTimePicker.MaximumDateTime
            ? DateTimePicker.MaximumDateTime
            : value;
    }

    private void SaveButtonClick(object? sender, EventArgs e)
    {
        if (AlarmTime <= DateTime.Now)
        {
            MessageBox.Show(
                this,
                "请选择一个未来的时间。",
                "时间无效",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
