using Avalonia.Controls;

namespace Descreen;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // Block Alt+F4 completely — only Settings → Quit can close the app
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = true;
        base.OnClosing(e);
    }
}
