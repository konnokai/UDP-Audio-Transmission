#pragma warning disable CS8618 // 退出建構函式時，不可為 Null 的欄位必須包含非 Null 值。請考慮宣告為可為 Null。

using Discord_Audio_Transmission.NetworkChat;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Discord_Audio_Transmission.Utils;
using System.Net;
using System.Net.Sockets;

namespace Discord_Audio_Transmission
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int recordDeviceNumber = 0, playerDeviceNumber = 0;
        private NetworkAudioSender networkAudioSender;
        private NetworkAudioPlayer networkAudioPlayer;

        public MainWindow()
        {

            InitializeComponent();

            if (WaveIn.DeviceCount <= 0)
            {
                MessageBox.Show(this, "沒有音效輸入設備\n請確認設備是否已插入或被停用", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                tab_Record.Dispatcher.Invoke(() => { tab_Record.IsEnabled = false; });
                tabControl.Dispatcher.Invoke(() => { tabControl.SelectedIndex = 1; });
            }
            else
            {
                cb_RecordDevice.Dispatcher.Invoke(new Action(() => { cb_RecordDevice.Items.Add("預設錄音裝置"); }));

                foreach (var item in GetInputAudioDevices())
                {
                    cb_RecordDevice.Dispatcher.Invoke(new Action(() => { cb_RecordDevice.Items.Add($"{item.Key}"); }));
                }

                cb_RecordDevice.Dispatcher.Invoke(new Action(() => { cb_RecordDevice.SelectedIndex = 0; }));
            }

            if (WaveOut.DeviceCount <= 0)
            {
                MessageBox.Show(this, "沒有音效輸出設備\n請確認設備是否已插入或被停用", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                tab_Player.Dispatcher.Invoke(() => { tab_Player.IsEnabled = false; });
            }
            else
            {

                cb_PlayerDevice.Dispatcher.Invoke(new Action(() => { cb_PlayerDevice.Items.Add("預設播放裝置"); }));

                foreach (var item in GetOutputAudioDevices())
                {
                    cb_PlayerDevice.Dispatcher.Invoke(new Action(() => { cb_PlayerDevice.Items.Add($"{item.Key}"); }));
                }

                cb_PlayerDevice.Dispatcher.Invoke(new Action(() => { cb_PlayerDevice.SelectedIndex = 0; }));
            }

            // use reflection to find all the codecs
            // NAudio Demo
            var codecs = ReflectionHelper.CreateAllInstancesOf<INetworkChatCodec>();
            PopulateCodecsCombo(codecs);
        }

        // NAudio Demo
        private void btn_StartRecord_Click(object sender, RoutedEventArgs e)
        {
            string[] strIPAndPort = txt_IPPort.Text.Split(':');
            string IP = strIPAndPort[0];
            int port;
            if (strIPAndPort.Length == 1 || !int.TryParse(strIPAndPort[1], out port))
            {
                port = FreeTcpPort();
                txt_IPPort.Dispatcher.Invoke(() => { txt_IPPort.Text = $"{IP}:{port}"; });
            }
            
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(IP), port);
            networkAudioSender = new NetworkAudioSender(((CodecComboItem)cb_Code.SelectedItem).Codec, recordDeviceNumber, new UdpAudioSender(endPoint));
            btn_StartRecord.Dispatcher.Invoke(() => { btn_StartRecord.IsEnabled = false; });
            btn_StopRecord.Dispatcher.Invoke(() => { btn_StopRecord.IsEnabled = true; });
        }

        private void btn_StopRecord_Click(object sender, RoutedEventArgs e)
        {
            if (networkAudioSender != null)
            {
                networkAudioSender.Dispose();
            }

            btn_StartRecord.Dispatcher.Invoke(() => { btn_StartRecord.IsEnabled = true; });
            btn_StopRecord.Dispatcher.Invoke(() => { btn_StopRecord.IsEnabled = false; });
        }


        // NAudio Demo
        private void btn_StartPlayer_Click(object sender, RoutedEventArgs e)
        {
            string[] strIPAndPort = txt_IPPort.Text.Split(':');
            string IP = strIPAndPort[0];
            int port;
            if (strIPAndPort.Length == 1 || !int.TryParse(strIPAndPort[1], out port))
            {
                port = FreeTcpPort();
                txt_IPPort.Dispatcher.Invoke(() => { txt_IPPort.Text = $"{IP}:{port}"; });
            }

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(IP), port);
            networkAudioPlayer = new NetworkAudioPlayer(((CodecComboItem)cb_Code.SelectedItem).Codec, playerDeviceNumber, new UdpAudioReceiver(endPoint));
            btn_StartPlayer.Dispatcher.Invoke(() => { btn_StartPlayer.IsEnabled = false; });
            btn_StopPlayer.Dispatcher.Invoke(() => { btn_StopPlayer.IsEnabled = true; });
        }

        private void btn_StopPlayer_Click(object sender, RoutedEventArgs e)
        {
            if (networkAudioPlayer != null)
            {
                networkAudioPlayer.Dispose();
            }

            btn_StartPlayer.Dispatcher.Invoke(() => { btn_StartPlayer.IsEnabled = true; });
            btn_StopPlayer.Dispatcher.Invoke(() => { btn_StopPlayer.IsEnabled = false; });
        }

        private void cb_Device_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            recordDeviceNumber = cb_RecordDevice.SelectedIndex - 1;
            playerDeviceNumber = cb_PlayerDevice.SelectedIndex - 1;
        }

        // https://stackoverflow.com/a/150974
        static int FreeTcpPort()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        /// <summary>
        /// Use this method to get full device name
        /// </summary>
        /// <returns></returns>
        // https://stackoverflow.com/questions/1449162/get-the-full-name-of-a-wavein-device
        // https://stackoverflow.com/questions/1449136/enumerate-recording-devices-in-naudio
        public Dictionary<string, MMDevice> GetInputAudioDevices()
        {
            Dictionary<string, MMDevice> retVal = new Dictionary<string, MMDevice>();
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            int waveInDevices = WaveIn.DeviceCount;
            int deviceIndex = 0;
            for (int waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++)
            {
                WaveInCapabilities deviceInfo = WaveIn.GetCapabilities(waveInDevice);
                foreach (MMDevice device in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
                {
                    if (device.FriendlyName.StartsWith(deviceInfo.ProductName))
                    {
                        deviceIndex++;
                        retVal.Add($"{deviceIndex}. {device.FriendlyName}", device);
                        break;
                    }
                }
            }

            return retVal;
        }

        /// <summary>
        /// Use this method to get full device name
        /// </summary>
        /// <returns></returns>
        // 同上，只是把WaveIn改成WaveOut而已
        public Dictionary<string, MMDevice> GetOutputAudioDevices()
        {
            Dictionary<string, MMDevice> retVal = new Dictionary<string, MMDevice>();
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            int waveOutDevices = WaveOut.DeviceCount;
            int deviceIndex = 0;
            for (int waveOutDevice = 0; waveOutDevice < waveOutDevices; waveOutDevice++)
            {
                WaveOutCapabilities deviceInfo = WaveOut.GetCapabilities(waveOutDevice);
                foreach (MMDevice device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                {
                    if (device.FriendlyName.StartsWith(deviceInfo.ProductName))
                    {
                        deviceIndex++;
                        retVal.Add($"{deviceIndex}. {device.FriendlyName}", device);
                        break;
                    }
                }
            }

            return retVal;
        }

        // NAudio Demo
        private void PopulateCodecsCombo(IEnumerable<INetworkChatCodec> codecs)
        {
            var sorted = from codec in codecs
                         where codec.IsAvailable
                         orderby codec.BitsPerSecond ascending
                         select codec;

            foreach (var codec in sorted)
            {
                var bitRate = codec.BitsPerSecond == -1 ? "VBR" : $"{codec.BitsPerSecond / 1000.0:0.#}kbps";
                var text = $"{codec.Name} ({bitRate})";
                cb_Code.Dispatcher.Invoke(() => { cb_Code.Items.Add(new CodecComboItem { Text = text, Codec = codec }); });
            }

            cb_Code.Dispatcher.Invoke(() => { cb_Code.SelectedIndex = 0; });
        }

        // NAudio Demo
        class CodecComboItem
        {
            public string Text { get; set; }
            public INetworkChatCodec Codec { get; set; }
            public override string ToString()
            {
                return Text;
            }
        }
    }
}
