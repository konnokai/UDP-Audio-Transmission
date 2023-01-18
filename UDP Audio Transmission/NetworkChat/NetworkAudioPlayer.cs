using NAudio.Extras;
using NAudio.Wave;
using System;

namespace UDP_Audio_Transmission.NetworkChat
{
    class NetworkAudioPlayer : IDisposable
    {
        private readonly INetworkChatCodec codec;
        private readonly IAudioReceiver receiver;
        private readonly WaveOut waveOut;
        private readonly BufferedWaveProvider waveProvider;

        public event EventHandler<FftEventArgs> FftCalculated;
        public event EventHandler<MaxSampleEventArgs> MaximumCalculated;

        public NetworkAudioPlayer(INetworkChatCodec codec, int outputDeviceNumber, IAudioReceiver receiver)
        {
            this.codec = codec;
            this.receiver = receiver;

            waveOut = new WaveOut();
            waveProvider = new BufferedWaveProvider(codec.RecordFormat);

            var aggregator = new SampleAggregator(waveProvider.ToSampleProvider());
            aggregator.NotificationCount = codec.RecordFormat.SampleRate / 100;
            aggregator.PerformFFT = true;
            aggregator.FftCalculated += (s, a) => FftCalculated?.Invoke(this, a);
            aggregator.MaximumCalculated += (s, a) => MaximumCalculated?.Invoke(this, a);

            receiver.OnReceived(OnDataReceived);

            waveOut.DeviceNumber = outputDeviceNumber;
            waveOut.Init(aggregator);
            waveOut.Play();
        }

        void OnDataReceived(byte[] compressed)
        {
            byte[] decoded = codec.Decode(compressed, 0, compressed.Length);
            waveProvider.AddSamples(decoded, 0, decoded.Length);
        }

        public void Dispose()
        {
            receiver?.Dispose();
            waveOut?.Dispose();
        }
    }
}