using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
    using System.IO;
    using System.Runtime.InteropServices;
using SilverCraft.CSCore.PortAudio;

using SilverCraft.CSCore.PortAudio.Native;

namespace HotwordDetectionApp
{
    
    public class AudioCapture
    {
        private static SilverCraft.CSCore.PortAudio.Native.PaStream _stream;
        private static Action<float[]> _onAudioCaptured;
        private static MemoryStream _audioBuffer = new MemoryStream();

        public static void StartCapture(Action<float[]> onAudioCaptured)
        {
            _onAudioCaptured=onAudioCaptured;
            PortAudio.Pa_Initialize();
            var inputParameters = new PortAudio.PaStreamParameters
            {
                device=PortAudio.Pa_GetDefaultInputDevice(),
                channelCount=1,
                sampleFormat=PortAudio.PaSampleFormat.paFloat32,
                suggestedLatency=PortAudio.Pa_GetDeviceInfo(PortAudio.Pa_GetDefaultInputDevice()).defaultLowInputLatency,
                hostApiSpecificStreamInfo=IntPtr.Zero
            };

            Pa_OpenStream(out _stream, ref inputParameters, IntPtr.Zero, 16000, 256, PortAudio.PaStreamFlags.paClipOff, Callback, IntPtr.Zero);
            PortAudio.Pa_StartStream(_stream);
        }

        private static PortAudio.PaStreamCallbackResult Callback(IntPtr input, IntPtr output, uint frameCount, ref PortAudio.PaStreamCallbackTimeInfo timeInfo, PortAudio.PaStreamCallbackFlags statusFlags, IntPtr userData)
        {
            var buffer = new float[frameCount];
            Marshal.Copy(input, buffer, 0, (int)frameCount);

            _onAudioCaptured?.Invoke(buffer);

            // Save the audio data to the buffer
            byte[] byteBuffer = new byte[buffer.Length*sizeof(float)];
            Buffer.BlockCopy(buffer, 0, byteBuffer, 0, byteBuffer.Length);
            _audioBuffer.Write(byteBuffer, 0, byteBuffer.Length);

            return PortAudio.PaStreamCallbackResult.paContinue;
        }

        public static void StopCapture()
        {
            PortAudio.Pa_StopStream(_stream);
            PortAudio.Pa_CloseStream(_stream);
            PortAudio.Pa_Terminate();
        }

        public static void PlayAudio()
        {
            _audioBuffer.Position=0;
            var outputParameters = new PortAudio.PaStreamParameters
            {
                device=PortAudio.Pa_GetDefaultOutputDevice(),
                channelCount=1,
                sampleFormat=PortAudio.PaSampleFormat.paFloat32,
                suggestedLatency=PortAudio.Pa_GetDeviceInfo(PortAudio.Pa_GetDefaultOutputDevice()).defaultLowOutputLatency,
                hostApiSpecificStreamInfo=IntPtr.Zero
            };

            PortAudio.Pa_OpenStream(out _stream, IntPtr.Zero, ref outputParameters, 16000, 256, PortAudio.PaStreamFlags.paClipOff, PlaybackCallback, IntPtr.Zero);
            PortAudio.Pa_StartStream(_stream);
        }

        private static PortAudio.PaStreamCallbackResult PlaybackCallback(IntPtr input, IntPtr output, uint frameCount, ref PortAudio.PaStreamCallbackTimeInfo timeInfo, PortAudio.PaStreamCallbackFlags statusFlags, IntPtr userData)
        {
            byte[] buffer = new byte[frameCount*sizeof(float)];
            int bytesRead = _audioBuffer.Read(buffer, 0, buffer.Length);

            if (bytesRead==0)
            {
                return PortAudio.PaStreamCallbackResult.paComplete;
            }

            Marshal.Copy(buffer, 0, output, bytesRead/sizeof(float));
            return PortAudio.PaStreamCallbackResult.paContinue;
        }
    }
}
