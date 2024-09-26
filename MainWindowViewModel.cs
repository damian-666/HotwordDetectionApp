using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkiaSharp;


namespace HotwordDetectionApp
{

  
        public class MainWindowViewModel : ObservableObject
        {        //Buffer Size = (Hotword Duration) * (Sample Rate)* (Bit Depth) / 8
        //For example, if the hotword duration is 1 second, the sample rate is 44100 Hz, and the bit depth is 16, the buffer size would be:
        const int BufferSize = 2*44100*16/80;
        
        // samples
                                             //      Keep in mind that this is just a rough estimate, and you may need to adjust the buffer size based on your specific requirements and performance considerations.

        // Existing code
  

            private readonly RingBuffer<float> _audioBuffer = new(BufferSize);
            private bool _isMonitoringEnabled;
            private SKBitmap _waveformBitmap;
            private int _waveformWidth = 500; // Example width
            private int _waveformHeight = 100; // Example height

            public WriteableBitmap WaveformBitmap { get; private set; }

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
                _waveformBitmap=new SKBitmap(_waveformWidth, _waveformHeight);
            }

            private void StartCapture()
            {
                AudioCapture.StartCapture(OnAudioCaptured, AudioCapture.SelectedMicIndex);
            }

            private void StopCapture()
            {
                AudioCapture.StopCapture();
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
                    // Implement monitoring start logic
                }
                else
                {
                    // Implement monitoring stop logic
                }
            }

        private void OnAudioCaptured(float[] audioData)
        {
            foreach (var sample in audioData)
            {
                _audioBuffer.Write(sample);
            }

            double SignalStrength = CalculateRMS(audioData);
            // Ensure UI updates are performed on the UI thread
            Dispatcher.UIThread.InvokeAsync(() => { 
            DrawWaveform(audioData);
            UpdateSignalBars(SignalStrength);
        }
        );
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
                using (var canvas = new SKCanvas(_waveformBitmap))
                {
                    canvas.Clear(SKColors.Black);

                    if (audioData.Length==0)
                    {
                        return;
                    }

                    float middle = _waveformHeight/2f;
                    float maxAmplitude = 100.0f; // Assuming the audio data is normalized between -1 and 1

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

                    // Update the Avalonia bitmap with the Skia bitmap
                    using (var data = _waveformBitmap.PeekPixels())
                    {
                        WaveformBitmap=new WriteableBitmap(new Avalonia.PixelSize(_waveformWidth, _waveformHeight), new Avalonia.Vector(96, 96), Avalonia.Platform.PixelFormat.Bgra8888);
                        using (var stream = WaveformBitmap.Lock())
                        {
                            data.ReadPixels(new SKImageInfo(_waveformWidth, _waveformHeight), stream.Address, data.RowBytes);

                    //        OnPropertyChanged(nameof(WaveformBitmap));
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


