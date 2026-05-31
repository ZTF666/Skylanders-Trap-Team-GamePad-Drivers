using System.Windows.Forms;
using SkylandersGamePad;

[STAThread]
Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
// Install sync context before TrayApp constructor so callbacks can marshal to UI thread
SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
Application.Run(new TrayApp());
