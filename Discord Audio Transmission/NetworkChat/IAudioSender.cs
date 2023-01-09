using System;

namespace Discord_Audio_Transmission.NetworkChat
{
    interface IAudioSender : IDisposable
    {
        void Send(byte[] payload);
    }
}