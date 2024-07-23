using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia;
using System.Text.Json;
using System.IO;
using ReactiveUI;

namespace HotwordDetectionApp;


public partial class MainWindow : Window
{
    private const string WindowStateFile = "windowstate.json";



    public MainWindow()
    {
        InitializeComponent();



#if DEBUG
        this.AttachDevTools();
#endif
        this.Closing+=MainWindow_Closing;
        this.Opened+=MainWindow_Opened;
#if DEBUG
            this.AttachDevTools();
#endif
        DataContext=new MainWindowViewModel();

        var hotwordButtonsPanel = this.FindControl<StackPanel>("HotwordButtonsPanel");
        var viewModel = (MainWindowViewModel)DataContext;

        foreach (var hotword in viewModel.Hotwords)
        {
            var button = new Button { Content=hotword, Command=ReactiveCommand.Create(() => RecordHotword(hotword)) };
            hotwordButtonsPanel.Children.Add(button);
        }
    }



    private void RecordHotword(string hotword)
    {
        // Implement hotword recording logic
    }
        private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void MainWindow_Opened(object? sender, System.EventArgs e)
    {
        if (File.Exists(WindowStateFile))
        {
            var state = File.ReadAllText(WindowStateFile);
            var windowState = JsonSerializer.Deserialize<WindowState>(state);
            if (windowState!=null)
            {
                this.Position=new PixelPoint(windowState.X, windowState.Y);
                this.Width=windowState.Width;
                this.Height=windowState.Height;
            }
        }
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        var windowState = new WindowState
        {
            X=this.Position.X,
            Y=this.Position.Y,
            Width=this.Width,
            Height=this.Height
        };
        var state = JsonSerializer.Serialize(windowState);
        File.WriteAllText(WindowStateFile, state);
    }

    private class WindowState
    {
        public int X { get; set; }
        public int Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }


  
}
