using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia;
using System.Text.Json;
using System.IO;
using Avalonia.Media;
using System;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using SkiaSharp;
using Avalonia.Threading;
using Avalonia.Controls.Shapes;
using PortAudioSharp;
using static PortAudioSharp.PortAudio;

namespace HotwordDetectionApp
{
    public partial class MainWindow : Window
    {
        private const string WindowStateFile = "windowstate.json";
        private const int BufferSize = 2*44100*16/80;

        private WriteableBitmap _waveformBitmap;
        public WriteableBitmap WaveformBitmap
        {
            get => _waveformBitmap;
            private set => _waveformBitmap=value;
        }

        private readonly RingBuffer<float> _audioBuffer = new(BufferSize);
        private bool _isMonitoringEnabled;
        private int _waveformWidth = 500;
        private int _waveformHeight = 100;

        public IAsyncRelayCommand StartCaptureCommand { get; }
        public IAsyncRelayCommand StopCaptureCommand { get; }
        public IAsyncRelayCommand PlayAudioCommand { get; }
        public IRelayCommand ToggleMonitoringCommand { get; }


        private ObservableCollection<string> _microphones;

        /// <summary>
        /// Collection of available microphones lazy   -loaded from PortAudio
        /// </summary>
        public ObservableCollection<string> Microphones
        {
            get
            {
                if (_microphones==null)
                {
                    _microphones=new ObservableCollection<string>();
                }
                return _microphones;
            }
        }

  
public ObservableCollection<string> Hotwords { get; }

        private ComboBox _microphoneComboBox;

        public MainWindow()
        {
            InitializeComponent();



#if DEBUG
          this.AttachDevTools();
#endif
            this.Closing+=MainWindow_Closing;
            this.Opened+=MainWindow_Opened;

       
               StartCaptureCommand =new AsyncRelayCommand(StartCapture);
            StopCaptureCommand=new AsyncRelayCommand(StopCapture);
            PlayAudioCommand=new AsyncRelayCommand(PlayAudio);
            ToggleMonitoringCommand=new RelayCommand(ToggleMonitoring);
            Hotwords=new ObservableCollection<string> { "left", "right", "up", "down" };
            WaveformBitmap=new WriteableBitmap(new Avalonia.PixelSize(_waveformWidth, _waveformHeight), new Avalonia.Vector(96, 96), Avalonia.Platform.PixelFormat.Bgra8888);

      
            
        }

        private void RefreshMicrophoneNames()
        {
            AudioCapture.RefreshMicrophoneNames();
            Microphones.Clear();
            foreach (var mic in AudioCapture.Mics)
            {

                Microphones.Add(mic);
            }
        }


        public override void BeginInit()
        {
            base.BeginInit();

            RefreshMicrophoneNames();


         
            
        }

        private void UpdateHotwords()

        { 

            }
       
        private void SelectDefaultMicrophone()
        {
            int defaultMicIndex = AudioCapture.SelectedMicIndex;
            if (defaultMicIndex>=0&&defaultMicIndex<Microphones.Count)
            {
                _microphoneComboBox.SelectedIndex=defaultMicIndex;
            }
        }

        private void MicrophoneComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_microphoneComboBox.SelectedIndex!=AudioCapture.SelectedMicIndex)
            {
                AudioCapture.SelectedMicIndex=_microphoneComboBox.SelectedIndex;
            }
        }

        private async Task StartCapture()
        {
            await Task.Run(() => AudioCapture.StartCapture(OnAudioCaptured, AudioCapture.SelectedMicIndex));
        }

        private async Task StopCapture()
        {
            try
            {
                await Task.Run(() => AudioCapture.StopCapture());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task PlayAudio()
        {
            await Task.Run(() => AudioCapture.PlayAudio(AudioCapture.SelectedMicIndex));
        }

        private void ToggleMonitoring()
        {
            _isMonitoringEnabled=!_isMonitoringEnabled;
            if (_isMonitoringEnabled)
            {
                StartCapture();
            }
            else
            {
                StopCapture();
            }
        }

        private void OnAudioCaptured(float[] audioData)
        {
            foreach (var sample in audioData)
            {
                _audioBuffer.Write(sample);
            }

            double signalStrength = CalculateRMS(audioData);
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                DrawWaveform(audioData);
                UpdateSignalBars(signalStrength);
            });
        }

        private double CalculateRMS(float[] audioData)
        {
            double sum = 0;
            foreach (var sample in audioData)
            {
                sum+=sample*sample;
            }
            return 1000*Math.Sqrt(sum/audioData.Length);
        }

        private void DrawWaveform(float[] audioData)
        {
            if (_waveformBitmap==null)
            {
                return;
            }

            using (var skBitmap = new SKBitmap(_waveformWidth, _waveformHeight))
            {
                using (var canvas = new SKCanvas(skBitmap))
                {
                    canvas.Clear(SKColors.Black);

                    if (audioData.Length==0)
                    {
                        return;
                    }

                    float middle = _waveformHeight/2f;
                    float maxAmplitude = 1.0f; // Assuming the audio data is normalized between -1 and 1

                    using (var paint = new SKPaint())
                    {
                        paint.Color=SKColors.Green;
                        paint.IsAntialias=true;
                        paint.StrokeWidth=1;

                        for (int i = 0; i<audioData.Length-1; i++)
                        {
                            float x1 = (i/(float)audioData.Length)*_waveformWidth;
                            float y1 = middle+(audioData[i]/maxAmplitude)*middle;
                            float x2 = ((i+1)/(float)audioData.Length)*_waveformWidth;
                            float y2 = middle+(audioData[i+1]/maxAmplitude)*middle;

                            canvas.DrawLine(x1, y1, x2, y2, paint);
                        }
                    }
                }

                using (var stream = _waveformBitmap.Lock())
                {
                    using (var skSurface = SKSurface.Create(new SKImageInfo(_waveformWidth, _waveformHeight), stream.Address, _waveformWidth*4))
                    {
                        var canvas = skSurface.Canvas;
                        canvas.DrawBitmap(skBitmap, 0, 0);
                    }
                }
            }
        }


        private void UpdateSignalBars(double rms)
        {
            var signalBar1 = this.FindControl<Rectangle>("SignalBar1");
            var signalBar2 = this.FindControl<Rectangle>("SignalBar2");
            var signalBar3 = this.FindControl<Rectangle>("SignalBar3");
            var signalBar4 = this.FindControl<Rectangle>("SignalBar4");
            var signalBar5 = this.FindControl<Rectangle>("SignalBar5");

            var logRMS = Math.Log10(rms+1)*20; // Example conversion
            signalBar1.Fill=logRMS>4 ? Brushes.Yellow : Brushes.Transparent;
            signalBar2.Fill=logRMS>8 ? Brushes.Yellow : Brushes.Transparent;
            signalBar3.Fill=logRMS>12 ? Brushes.Yellow : Brushes.Transparent;
            signalBar4.Fill=logRMS>16 ? Brushes.Yellow : Brushes.Transparent;
            signalBar5.Fill=logRMS>20 ? Brushes.Red : Brushes.Transparent;
        }

        private async Task RecordHotword(string hotword)
        {

            await Task.Run(() =>
            {
             
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
    }
}