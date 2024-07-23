using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
    using System.IO;
    using System.Runtime.InteropServices;


    using PortAudio = SharperPortAudio.Base;


namespace HotwordDetectionApp
{
    



    public class AudioCapture
    {
        private static PortAudio.Stream stream;
        private static Action<float[]> _onAudioCaptured;
        private static MemoryStream _audioBuffer = new MemoryStream();

        public static void StartCapture(Action<float[]> onAudioCaptured)
        {
            _onAudioCaptured=onAudioCaptured;
            PortAudio.PortAudio.Initialize();
            var inputParameters = new PortAudio.StreamParameters()
            {

                channelCount=1,
                sampleFormat=PortAudio.SampleFormat.Float32,
                suggestedLatency=PortAudio.PortAudio.GetDeviceInfo(PortAudio.PortAudio.DefaultInputDevice).defaultLowInputLatency,
                hostApiSpecificStreamInfo=0
            };

            PortAudio.PortAudio.Initialize(out _stream, ref inputParameters, IntPtr.Zero, 16000, 256, PortAudio.PaStreamFlags.paClipOff, Callback, IntPtr.Zero);
            PortAudio.PortAudio.StreamReader = new StartStream(_stream);
        }

        private static PortAudio.StreamCallbackResult Callback(IntPtr input, IntPtr output, uint frameCount, ref PortAudio.StreamCallbackTimeInfo timeInfo, PortAudio.StreamCallbackFlags statusFlags, IntPtr userData)
        {
            var buffer = new float[frameCount];
            Marshal.Copy(input, buffer, 0, (int)frameCount);

            _onAudioCaptured?.Invoke(buffer);

            // Save the audio data to the buffer
            byte[] byteBuffer = new byte[buffer.Length*sizeof(float)];
            Buffer.BlockCopy(buffer, 0, byteBuffer, 0, byteBuffer.Length);
            _audioBuffer.Write(byteBuffer, 0, byteBuffer.Length);

            return PortAudio.StreamCallbackResult.Continue;
        }

        public static void StopCapture()
        {


            PortAudio.PortAudio.StopStream(_stream);
            PortAudio.CloseStream(_stream);
            PortAudio.PortAudio.Terminate();
        }

        public static void PlayAudio()
        {
            _audioBuffer.Position=0;
            var outputParameters = new PortAudio.PaStreamParameters
            {
                device=PortAudio.PortAudio.GetDefaultOutputDevice(),
                channelCount=1,
                sampleFormat=PortAudio.SampleFormat.Float32,
                suggestedLatency=PortAudio.PortAudio.GetDeviceInfo(DefaultOutputDevice).defaultLowOutputLatency,
                hostApiSpecificStreamInfo=IntPtr.Zero
            };

            PortAudio.Pa_OpenStream(out _stream, IntPtr.Zero, ref outputParameters, 16000, 256, PortAudio.PaStreamFlags.paClipOff, PlaybackCallback, IntPtr.Zero);
            PortAudio.Pa_StartStream(_stream);
        }

        private static PortAudio.StreamCallbackResult PlaybackCallback(IntPtr input, IntPtr output, uint frameCount, ref PortAudio.StreamCallbackTimeInfo timeInfo, PortAudio.StreamCallbackFlags statusFlags, IntPtr userData)
        {
            byte[] buffer = new byte[frameCount*sizeof(float)];
            int bytesRead = _audioBuffer.Read(buffer, 0, buffer.Length);

            if (bytesRead==0)
            {
                return PortAudio.StreamCallbackResult.Complete;
            }

            Marshal.Copy(buffer, 0, output, bytesRead/sizeof(float));
            return PortAudio.StreamCallbackResult.Continue;
        }
    }
}
