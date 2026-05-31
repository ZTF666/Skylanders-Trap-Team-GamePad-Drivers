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
        SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
        Application.Run(new TrayApp());
    }
}
