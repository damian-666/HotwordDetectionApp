using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
    using System.IO;
    using System.Runtime.InteropServices;


//using static PortAudio.Bindings.PaBinding;
  using PortAudioSharp;
using static PortAudioSharp.PortAudio;

namespace HotwordDetectionApp           
{


    public static class AudioCaptureManager
    {
        private static PortAudioSharp.Stream _stream;
        private static Action<float[]>? _onAudioCaptured;
        private static MemoryStream _audioBuffer = new MemoryStream();
        private static StreamParameters? outputParameters;

        public static void StartCapture(Action<float[]> onAudioCaptured, int device)
        {



            _onAudioCaptured=onAudioCaptured;
            PortAudio.Initialize();


            //  set up      the input parameters
            var inputParameters = new StreamParameters {
                device=device,
                channelCount=1,
                sampleFormat=SampleFormat.Float32,
                suggestedLatency=PortAudio.GetDeviceInfo(device).defaultLowInputLatency,
                hostApiSpecificStreamInfo=IntPtr.Zero


        };

             _stream =
                new PortAudioSharp.Stream(inParams: inputParameters,
                outputParameters,  sampleRate: 16000,
                framesPerBuffer: 256, streamFlags: StreamFlags.ClipOff, callback: Callback, userData: IntPtr.Zero);
       
         _stream.Start();
            Console.WriteLine(inputParameters);
            Console.WriteLine("Started! Please speak");
      //      S(, outputParameters, 16000, 256, StreamFlags.ClipOff, Callback,IntPtr.Zero);
  
       
   
        }
     //  callback = (IntPtr input, IntPtr output,
     //UInt32 frameCount,
     //ref StreamCallbackTimeInfo timeInfo,
     //StreamCallbackFlags statusFlags,
     //IntPtr userData
     //) =>
     //   {
     //       float[] samples = new float[frameCount];
     //       Marshal.Copy(input, samples, 0, (Int32)frameCount);

     //       s.AcceptWaveform(config.FeatConfig.SampleRate, samples);

     //       return StreamCallbackResult.Continue;
     //   }; 

    static StreamCallbackResult Callback(IntPtr input, IntPtr output, uint frameCount, ref StreamCallbackTimeInfo timeInfo,
             StreamCallbackFlags statusFlags, IntPtr userData)
        {
            var buffer = new float[frameCount];
            Marshal.Copy(input, buffer, 0, (int)frameCount);

            _onAudioCaptured?.Invoke(buffer);

            // Save the audio data to the buffer
            byte[] byteBuffer = new byte[buffer.Length*sizeof(float)];
            Buffer.BlockCopy(buffer, 0, byteBuffer, 0, byteBuffer.Length);
            _audioBuffer.Write(byteBuffer, 0, byteBuffer.Length);

            return StreamCallbackResult.Continue;
        }

        public static void StopCapture()
        {
            _stream.Stop();
            _stream.Close();
            PortAudio.Terminate();
        }

        /// <summary>
        /// play audio
        /// </summary>
        public static void PlayAudio(int deviceIndex)
        {
            _audioBuffer.Position=0;

            StreamParameters param = new StreamParameters();
            param.device=deviceIndex;
            param.channelCount=1;
            param.sampleFormat=SampleFormat.Float32;
            param.suggestedLatency=PortAudio.GetDeviceInfo(deviceIndex).defaultLowInputLatency;
          
            param.hostApiSpecificStreamInfo=IntPtr.Zero;
            _stream.Start(); 
        }

        private static StreamCallbackResult PlaybackCallback(IntPtr input, IntPtr output, uint frameCount, ref StreamCallbackTimeInfo timeInfo, StreamCallbackFlags statusFlags, IntPtr userData)
        {
            byte[] buffer = new byte[frameCount*sizeof(float)];
            int bytesRead = _audioBuffer.Read(buffer, 0, buffer.Length);

            if (bytesRead==0)
            {
                return StreamCallbackResult.Complete;
            }

            Marshal.Copy(buffer, 0, output, bytesRead/sizeof(float));
            return StreamCallbackResult.Continue;
        }
    }
}