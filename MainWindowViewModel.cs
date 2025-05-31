using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PortAudioSharp;
using SkiaSharp;

namespace HotwordDetectionApp
{
    public class MainWindowViewModel : ObservableObject
    {
        private const int BufferSize = 2*44100*16/80;

        private WriteableBitmap _waveformBitmap;
        public WriteableBitmap WaveformBitmap
        {
            get => _waveformBitmap;
            private set => SetProperty(ref _waveformBitmap, value);
        }

        private readonly RingBuffer<float> _audioBuffer = new(BufferSize);
        private bool _isMonitoringEnabled;
        private int _waveformWidth = 500;
        private int _waveformHeight = 100;

        public IRelayCommand StartCaptureCommand { get; }
        public IRelayCommand StopCaptureCommand { get; }
        public IRelayCommand PlayAudioCommand { get; }
        public IRelayCommand ToggleMonitoringCommand { get; }

        public ObservableCollection<string> Hotwords { get; }

        public MainWindowViewModel()
        {
            StartCaptureCommand=new RelayCommand(StartCapture);
            StopCaptureCommand=new RelayCommand(StopCapture);
            PlayAudioCommand=new RelayCommand(PlayAudio);
            ToggleMonitoringCommand=new RelayCommand(ToggleMonitoring);
            Hotwords=new ObservableCollection<string> { "left", "right", "up", "down" };
            WaveformBitmap=new WriteableBitmap(new Avalonia.PixelSize(_waveformWidth, _waveformHeight), new Avalonia.Vector(96, 96), Avalonia.Platform.PixelFormat.Bgra8888);
            Hotwords.CollectionChanged += (sender, args) => UpdateHotwords();
        }


        private void StartCapture()
        {
            AudioCapture.StartCapture(OnAudioCaptured, AudioCapture.SelectedMicIndex);
        }

        public void StopCapture()
        {
            try
            {
                AudioCapture.StopCapture();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void PlayAudio()
        {
            AudioCapture.PlayAudio(AudioCapture.SelectedMicIndex);
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

            double SignalStrength = CalculateRMS(audioData);
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                DrawWaveform(audioData);
                UpdateSignalBars(SignalStrength);
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
                    float maxAmplitude = 100.0f;

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
                OnPropertyChanged(nameof(WaveformBitmap));
            }
        }

        private void UpdateSignalBars(double rms)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop&&desktop.MainWindow is MainWindow mainWindow)
            {

                if (mainWindow.SignalBar1==null)
                    return;
             var logRMS = Math.Log10(rms+1)*20;
                mainWindow.SignalBar1.Fill=logRMS>4 ? Brushes.Yellow : Brushes.Transparent;
                mainWindow.SignalBar2.Fill=logRMS>8 ? Brushes.Yellow : Brushes.Transparent;
                mainWindow.SignalBar3.Fill=logRMS>12 ? Brushes.Yellow : Brushes.Transparent;
                mainWindow.SignalBar4.Fill=logRMS>16 ? Brushes.Yellow : Brushes.Transparent;
                mainWindow.SignalBar5.Fill=logRMS>20 ? Brushes.Red : Brushes.Transparent;
            }
        }

        // Add the missing UpdateHotwords method
        private void UpdateHotwords()
        {
             // Implementation for updating hotwords
            // This could be updating a UI element or processing the hotwords in some way
        }
    }
}
