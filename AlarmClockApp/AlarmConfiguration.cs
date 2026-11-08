namespace AlarmClockApp;

internal enum AlarmStatus
{
    None,
    Armed,
    Ringing,
    Snoozed
}

internal sealed class AlarmConfiguration
{
    public DateTime? AlarmTime { get; set; }

    public string Note { get; set; } = string.Empty;

    public AlarmStatus Status { get; set; }

    public bool IsRead { get; set; }
}
