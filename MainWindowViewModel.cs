
     using System;
    using System.Collections.ObjectModel;
    using System.Reactive;
    using System.Windows.Input;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Interactivity;
    using Avalonia.Media;
using ReactiveUI;


namespace HotwordDetectionApp
{

    public class MainWindowViewModel : ReactiveObject
    {
        public ICommand StartCaptureCommand { get; }
        public ICommand StopCaptureCommand { get; }
        public ICommand PlayAudioCommand { get; }

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

        public MainWindowViewModel()
        {
            StartCaptureCommand=ReactiveCommand.Create(StartCapture);
            StopCaptureCommand=ReactiveCommand.Create(StopCapture);
            PlayAudioCommand=ReactiveCommand.Create(PlayAudio);

            Hotwords=new ObservableCollection<string> { "left", "right", "up", "down" };
        }

        private void StartCapture()
        {
            // AudioCapture.StartCapture(OnAudioCaptured);
            MicName="Default Microphone";
            SignalStrength=0.5; // Example value, should be updated with actual signal strength
        }

        private void StopCapture()
        {
            // AudioCapture.StopCapture();
        }

        private void PlayAudio()
        {
            //  AudioCapture.PlayAudio();
        }

        private void OnAudioCaptured(float[] audioData)
        {
            // Update signal strength and waveform here
            SignalStrength=CalculateRMS(audioData);
            DrawWaveform(audioData);
            //    UpdateSignalBars(SignalStrength);
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
            // Implement waveform drawing logic here
        }

        private static double CalculateRMS(float[] audioData)
        {
            double sum = 0;
            foreach (var sample in audioData)
            {
                sum+=sample*sample;
            }
            return Math.Sqrt(sum/audioData.Length);
        }

        //private void DrawWaveform(float[] audioData)
        //{
        //    // Implement waveform drawing logic here
        //}

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