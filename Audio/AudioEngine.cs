using System;
using System.Runtime.InteropServices;
using PortAudio.Audio.Bindings;

namespace HotwordDetectionApp.Audio
{
    class AudioEngine
    {
        private const int FramesPerBuffer = 0; // paFramesPerBufferUnspecified
        private const PaBinding.PaStreamFlags StreamFlags = PaBinding.PaStreamFlags.paNoFlag;
        public readonly int channels;
        public readonly int sampleRate;
        public readonly double latency;
        private readonly nint stream;
        private bool disposed;

        public readonly AudioDevice device;

        public AudioEngine(AudioDevice device, int channels, int sampleRate, double latency)
        {
            this.device=device;
            this.channels=channels;
            this.sampleRate=sampleRate;
            this.latency=latency;

            var parameters = new PaBinding.PaStreamParameters
            {
                channelCount=channels,
                device=device.DeviceIndex,
                hostApiSpecificStreamInfo=nint.Zero,
                sampleFormat=PaBinding.PaSampleFormat.paFloat32,
                suggestedLatency=latency
            };

            nint stream;

            unsafe
            {
                PaBinding.PaStreamParameters tempParameters;
                var parametersPtr = new nint(&tempParameters);
                Marshal.StructureToPtr(parameters, parametersPtr, false);

                var code = PaBinding.Pa_OpenStream(
                    new nint(&stream),
                    nint.Zero,
                    parametersPtr,
                    sampleRate,
                    FramesPerBuffer,
                    StreamFlags,
                    null,
                    nint.Zero
                );

                PaBinding.MaybeThrow(code);
            }

            this.stream=stream;

            PaBinding.MaybeThrow(PaBinding.Pa_StartStream(stream));
        }

        public void Send(Span<float> samples)
        {
            unsafe
            {
                fixed (float* buffer = samples)
                {
                    var frames = samples.Length/channels;
                    PaBinding.Pa_WriteStream(stream, (nint)buffer, frames);
                }
            }
        }

        public void Dispose()
        {
            if (disposed||stream==nint.Zero)
            {
                return;
            }
            PaBinding.Pa_AbortStream(stream);
            PaBinding.Pa_CloseStream(stream);
            disposed=true;
        }
    }
}
