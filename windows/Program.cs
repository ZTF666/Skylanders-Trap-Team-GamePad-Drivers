using System.Windows.Forms;
using SkylandersGamePad;

namespace SkylandersGamePad;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ShowError(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => ShowError((Exception)e.ExceptionObject);

        SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());

        try
        {
            Application.Run(new TrayApp());
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private static void ShowError(Exception ex)
    {
        // Check for ViGEmBus not installed — the most common failure on a fresh machine
        bool vigemMissing =
            ex is FileNotFoundException ||
            ex.Message.Contains("ViGEm",      StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("vigembus",   StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("bus driver", StringComparison.OrdinalIgnoreCase);

        string message = vigemMissing
            ? "ViGEmBus driver is not installed.\n\n" +
              "Please download and install it from:\n" +
              "https://github.com/nefarius/ViGEmBus/releases\n\n" +
              "After installing, reboot your PC and run this app again."
            : $"Skylanders GamePad encountered an error:\n\n{ex.Message}";

        MessageBox.Show(message, "Skylanders GamePad", MessageBoxButtons.OK, MessageBoxIcon.Error);
        Environment.Exit(1);
    }
}
