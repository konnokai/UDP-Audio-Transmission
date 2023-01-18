using System;

namespace UDP_Audio_Transmission.NetworkChat
{
    interface IAudioSender : IDisposable
    {
        void Send(byte[] payload);
    }
}