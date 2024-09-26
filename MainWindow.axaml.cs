using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia;
using System.Text.Json;
using System.IO;

using Avalonia.Media;
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using SkiaSharp;
using Avalonia.Threading;
using Avalonia.Controls.Shapes;
namespace HotwordDetectionApp
{

    public partial class MainWindow : Window
    {
        private const string WindowStateFile = "windowstate.json";

        public WriteableBitmap WaveformBitmap { get; private set; }

        public IAsyncRelayCommand StartCaptureCommand { get; }
        public IAsyncRelayCommand StopCaptureCommand { get; }
        public IAsyncRelayCommand PlayAudioCommand { get; }
        public IRelayCommand ToggleMonitoringCommand { get; }


 
        public ObservableCollection<string> Hotwords { get; }


        public MainWindow()
        {
            InitializeComponent();

#if DEBUG
            this.AttachDevTools();
#endif
            this.Closing+=MainWindow_Closing;
            this.Opened+=MainWindow_Opened;

            DataContext=new MainWindowViewModel();

            var hotwordButtonsPanel = this.FindControl<StackPanel>("HotwordButtonsPanel");
            var viewModel = (MainWindowViewModel)DataContext;

            foreach (var hotword in viewModel.Hotwords)
            {
                var button = new Button { Content=hotword };
                button.Command=new AsyncRelayCommand(async () => await RecordHotword(hotword));
                hotwordButtonsPanel?.Children.Add(button);
            }

            // Initialize SignalBars
            SignalBar1=this?.FindControl<Rectangle>("SignalBar1");
            SignalBar2=this?.FindControl<Rectangle>("SignalBar2");
            SignalBar3=this.FindControl<Rectangle>("SignalBar3");
            SignalBar4=this.FindControl<Rectangle>("SignalBar4");
            SignalBar5=this.FindControl<Rectangle>("SignalBar5");
        }

        private async Task RecordHotword(string hotword)
        {
            await Task.Run(() =>
            {
                // Implement hotword recording logic
            });
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
                var windowState = JsonSerializer.Deserialize<WindowStateData>(state);
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
            var windowState = new WindowStateData
            {
                X=this.Position.X,
                Y=this.Position.Y,
                Width=this.Width,
                Height=this.Height
            };
            var state = JsonSerializer.Serialize(windowState);
            File.WriteAllText(WindowStateFile, state);
        }

        public class WindowStateData
        {
            public int X { get; set; }
            public int Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
        }
        public class MainWindowViewModel : ObservableObject
        {
            private string _micName = "NO MIC";
            public string MicName
            {
                get => _micName;
                set => SetProperty(ref _micName, value);
            }

            private double _signalStrength;
            public double SignalStrength
            {
                get => _signalStrength;
                set => SetProperty(ref _signalStrength, value);
            }

            private bool isMonitoringEnabled;
            private SKBitmap waveformBitmap;
            private int waveformWidth = 500; // Example width
            private int waveformHeight = 100; // Example height

            public WriteableBitmap WaveformBitmap { get; private set; }

            public IAsyncRelayCommand StartCaptureCommand { get; }
            public IAsyncRelayCommand StopCaptureCommand { get; }
            public IAsyncRelayCommand PlayAudioCommand { get; }
            public RelayCommand ToggleMonitoringCommand { get; }

            public ObservableCollection<string> Hotwords { get; }

            // Add the missing field
            private int _selectedDeviceIndex;
            public int SelectedDeviceIndex
            {
                get => _selectedDeviceIndex;
                set => SetProperty(ref _selectedDeviceIndex, value);
            }

            public MainWindowViewModel()
            {
                StartCaptureCommand=new AsyncRelayCommand(StartCapture);
                StopCaptureCommand=new AsyncRelayCommand(StopCapture);
                PlayAudioCommand=new AsyncRelayCommand(PlayAudio);
                ToggleMonitoringCommand=new RelayCommand(ToggleMonitoring);
                Hotwords=new ObservableCollection<string> { "left", "right", "up", "down" };
                WaveformBitmap=new WriteableBitmap(new Avalonia.PixelSize(waveformWidth, waveformHeight), new Avalonia.Vector(96, 96), Avalonia.Platform.PixelFormat.Bgra8888);
                waveformBitmap=new SKBitmap(waveformWidth, waveformHeight);
            }

            private async Task StartCapture()
            {
                await Task.Run(() => AudioCapture.StartCapture(async audioData => await OnAudioCaptured(audioData, AudioCapture.SelectedMicIndex), AudioCapture.SelectedMicIndex));
            }

            private async Task StopCapture()
            {
                await Task.Run(() =>
                {
                    AudioCapture.StopCapture();
                });
            }

            private async Task PlayAudio()
            {
                await Task.Run(() =>
                {
                    AudioCapture.PlayAudio(AudioCapture.SelectedMicIndex);
                });
            }

            private float[] FetchAudioData()
            {
                // Implement the logic to fetch audio data here
                // This is a placeholder method and should be replaced with actual implementation
                return new float[0];
            }

            private   void ToggleMonitoring()
            {
                isMonitoringEnabled=!isMonitoringEnabled;
                if (isMonitoringEnabled)
                {
                    // Implement monitoring start logic
                }
                else
                {
                    // Implement monitoring stop logic
                }
            }

            private async Task OnAudioCaptured(float[] audioData, int selectedMicIndex)
            {
                // Update signal strength and waveform here

                SignalStrength=await Task.Run(() => CalculateRMS(audioData));
                DrawWaveform(audioData);
               UpdateSignalBars(SignalStrength);
            }

            private double CalculateRMS(float[] audioData)
            {
                double sum = 0;
                foreach (var sample in audioData)
                {
                    sum+=sample*sample;
                }
                return Math.Sqrt(sum/audioData.Length);
            }

            void DrawWaveform(float[] audioData)
            {
                using (var canvas = new SKCanvas(waveformBitmap))
                {
                    canvas.Clear(SKColors.Black);

                    if (audioData.Length==0)
                    {
                        return;
                    }

                    float middle = waveformHeight/2f;
                    float maxAmplitude = 1.0f; // Assuming the audio data is normalized between -1 and 1

                    using (var paint = new SKPaint())
                    {
                        paint.Color=SKColors.Green;
                        paint.IsAntialias=true;
                        paint.StrokeWidth=1;

                        for (int i = 0; i<audioData.Length-1; i++)
                        {
                            float x1 = (i/(float)audioData.Length)*waveformWidth;
                            float y1 = middle+(audioData[i]/maxAmplitude)*middle;
                            float x2 = ((i+1)/(float)audioData.Length)*waveformWidth;
                            float y2 = middle+(audioData[i+1]/maxAmplitude)*middle;

                            canvas.DrawLine(x1, y1, x2, y2, paint);
                        }
                    }

                    // Update the Avalonia bitmap with the Skia bitmap
                    using (var data = waveformBitmap.PeekPixels())
                    {
                        WaveformBitmap=new WriteableBitmap(new Avalonia.PixelSize(waveformWidth, waveformHeight), new Avalonia.Vector(96, 96), Avalonia.Platform.PixelFormat.Bgra8888);
                        using (var stream = WaveformBitmap.Lock())
                        {
                            data.ReadPixels(new SKImageInfo(waveformWidth, waveformHeight), stream.Address, data.RowBytes);

                            OnPropertyChanged(nameof(WaveformBitmap));
                        }
                    }

                    OnPropertyChanged(nameof(WaveformBitmap));
                }
            }

            private void UpdateSignalBars(double rms)
            {
                // Get the current MainWindow instance
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop&&desktop.MainWindow is MainWindow mainWindow)
                {
                    // Convert RMS to logarithmic scale and update signal bars
                    var logRMS = Math.Log10(rms+1)*20; // Example conversion
                    mainWindow.SignalBar1.Fill=logRMS>4 ? Brushes.Yellow : Brushes.Transparent;
                    mainWindow.SignalBar2.Fill=logRMS>8 ? Brushes.Yellow : Brushes.Transparent;
                    mainWindow.SignalBar3.Fill=logRMS>12 ? Brushes.Yellow : Brushes.Transparent;
                    mainWindow.SignalBar4.Fill=logRMS>16 ? Brushes.Yellow : Brushes.Transparent;
                    mainWindow.SignalBar5.Fill=logRMS>20 ? Brushes.Red : Brushes.Transparent;
                }
            }
        }
    }
}
     

