using Avalonia.Controls;

namespace NetMana.Views;

public partial class MainWindow : Window
{
    private bool _isShuttingDown = false;
    public MainWindow()
    {
        InitializeComponent();
    }
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // If the user explicitly clicked "Quit" from the tray, let it close
        if (_isShuttingDown)
        {
            base.OnClosing(e);
            return;
        }

        // Otherwise, intercept the close (like pressing the top right X button)
        e.Cancel = true; 
        this.Hide(); // Hides it from the Linux taskbar, leaving only the tray icon active
    }
    public void CompletelyShutdownApp()
    {
        _isShuttingDown = true;
        this.Close();
    }
}