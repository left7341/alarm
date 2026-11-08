namespace AlarmClockApp;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ReportFatalError(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception exception)
            {
                ReportFatalError(exception);
            }
        };

        using var mutex = new Mutex(initiallyOwned: true, "AlarmClockApp.SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "任务栏闹钟已经在运行。请从系统托盘打开现有实例。",
                "任务栏闹钟",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        try
        {
            Application.Run(new MainForm());
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    private static void ReportFatalError(Exception exception)
    {
        AppLogger.Log(exception);
        MessageBox.Show(
            $"程序遇到未处理的错误，详细信息已写入日志：\n{AppLogger.LogPath}\n\n{exception.Message}",
            "任务栏闹钟 - 错误",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
