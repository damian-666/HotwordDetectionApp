using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Windows.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ReactiveUI;
using SkiaSharp;
using SkiaSharp.Views.Avalonia;

namespace HotwordDetectionApp
{

    public class MainWindowViewModel : ReactiveObject
    {
        public ICommand StartCaptureCommand { get; }
        public ICommand StopCaptureCommand { get; }
        public ICommand PlayAudioCommand { get; }
        public ICommand ToggleMonitoringCommand { get; }

        private string _micName = "NO MIC";
        public string MicName
        {
            get => _micName;
            set => this.RaiseAndSetIfChanged(ref _micName, value);
        }

        private double _signalStrength;
        public double SignalStrength
        {
            get => _signalStrength;
            set => this.RaiseAndSetIfChanged(ref _signalStrength, value);
        }

        public ObservableCollection<string> Hotwords { get; set; }

        private bool isMonitoringEnabled;
        private SKBitmap waveformBitmap;
        private int waveformWidth = 500; // Example width
        private int waveformHeight = 100; // Example height

        public WriteableBitmap WaveformBitmap { get; private set; }

        public MainWindowViewModel()
        {
            StartCaptureCommand=ReactiveCommand.Create(StartCapture);
            StopCaptureCommand=ReactiveCommand.Create(StopCapture);
            PlayAudioCommand=ReactiveCommand.Create(PlayAudio);
            ToggleMonitoringCommand=ReactiveCommand.Create(ToggleMonitoring);

            Hotwords=new ObservableCollection<string> { "left", "right", "up", "down" };
            waveformBitmap=new SKBitmap(waveformWidth, waveformHeight);
            WaveformBitmap=new WriteableBitmap(new Avalonia.PixelSize(waveformWidth, waveformHeight), new Avalonia.Vector(96, 96), Avalonia.Platform.PixelFormat.Bgra8888);
        }

        private void StartCapture()
        {
            // AudioCapture.StartCapture(OnAudioCaptured);
            MicName="Default Microphone";
            SignalStrength=0.5; // Example value, should be updated with actual signal strength
        }

        private void StopCapture()
        {
            AudioCapture.StopCapture();
        }

        private void PlayAudio()
        {
            // Implement your audio playback logic here
            AudioCapture.PlayAudio();
            // Example: Trigger waveform update during playback
            // You need to hook this up with your actual audio playback library
            DispatcherTimer timer = new DispatcherTimer
            {
                Interval=TimeSpan.FromMilliseconds(50) // Update every 50ms
            };
            timer.Tick+=(sender, args) => {
                // This should be replaced with actual audio data fetching during playback
                float[] audioData = AudioCapture.GetAudioData();
                OnAudioCaptured(audioData);
            };
            timer.Start();
        }

        private void ToggleMonitoring()
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

        private void OnAudioCaptured(float[] audioData)
        {
            // Update signal strength and waveform here
            SignalStrength=CalculateRMS(audioData);
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

        private void DrawWaveform(float[] audioData)
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
            }

            // Update the Avalonia bitmap with the Skia bitmap
            using (var data = waveformBitmap.PeekPixels())
            {
                WaveformBitmap=new WriteableBitmap(new Avalonia.PixelSize(waveformWidth, waveformHeight), new Avalonia.Vector(96, 96), Avalonia.Platform.PixelFormat.Bgra8888);
                using (var stream = WaveformBitmap.Lock())
                {
                    data.SaveTo(stream.Address, data.RowBytes, data.Height);
                }
            }

            this.RaisePropertyChanged(nameof(WaveformBitmap));
        }

        private void UpdateSignalBars(double rms)
        {
            // Convert RMS to logarithmic scale and update signal bars
            var logRMS = Math.Log10(rms+1)*20; // Example conversion
            SignalBar1.Fill=logRMS>4 ? Brushes.Yellow : Brushes.Transparent;
            SignalBar2.Fill=logRMS>8 ? Brushes.Yellow : Brushes.Transparent;
            SignalBar3.Fill=logRMS>12 ? Brushes.Yellow : Brushes.Transparent;
            SignalBar4.Fill=logRMS>16 ? Brushes.Yellow : Brushes.Transparent;
            SignalBar5.Fill=logRMS>20 ? Brushes.Red : Brushes.Transparent;
        }
    }
}
