using System;

namespace UDP_Audio_Transmission.NetworkChat
{
    interface IAudioReceiver : IDisposable
    {
        void OnReceived(Action<byte[]> handler);
    }
}