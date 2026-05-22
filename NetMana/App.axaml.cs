using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Input;
using NetMana.ViewModels;
using NetMana.Views;

namespace NetMana;

public partial class App : Application
{
    // Keep a direct reference to your main window instance
    private MainWindow? _mainWindow;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        // This sets the DataContext of App.axaml to itself, satisfying the tray icon bindings
        DataContext = this; 
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Instantiating MainWindow using your original Views and ViewModels setup
            _mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
            
            desktop.MainWindow = _mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    // --- System Tray Commands ---

    [RelayCommand]
    public void ShowWindow()
    {
        if (_mainWindow is null) return;

        // 1. Find the screen where the main window currently lives (or the primary monitor)
        var currentScreen = _mainWindow.Screens.ScreenFromWindow(_mainWindow) 
                            ?? _mainWindow.Screens.Primary;

        if (currentScreen is not null)
        {
            var workingArea = currentScreen.WorkingArea;
            var bounds = currentScreen.Bounds;
            
            int x = workingArea.X + workingArea.Width - (int)_mainWindow.Width - 20;
            int y = workingArea.Y + 10;
            
            
            // Smart-detect taskbar position adjustments
            if (workingArea.Height < bounds.Height) 
            {
                // Taskbar is at the TOP of the screen
                if (workingArea.Y > 0) 
                {
                    y = workingArea.Y + 10;
                }
            }
            else if (workingArea.Width < bounds.Width)
            {
                // Taskbar is on the LEFT side of the screen
                if (workingArea.X > 0)
                {
                    x = workingArea.X + 10;
                }
            }

            // Apply calculated raw pixel coordinates directly to your window
            _mainWindow.Position = new PixelPoint(x, y);
        }

        // 2. Unhide and force focus onto the window
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    // Fixed: Named 'OpenSettings' so CommunityToolkit accurately generates 'OpenSettingsCommand' 
    // to match your App.axaml binding exactly.
    [RelayCommand]
    public void OpenSettings()
    {
        // Example: Open a settings window or handle logic here
        Console.WriteLine("Settings opened!");
    }

    [RelayCommand]
    public void Quit()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Tell your MainWindow close interceptor that it is okay to fully shut down
            if (_mainWindow is MainWindow mainWin)
            {
                mainWin.CompletelyShutdownApp();
            }
            else
            {
                desktop.Shutdown();
            }
        }
    }
}