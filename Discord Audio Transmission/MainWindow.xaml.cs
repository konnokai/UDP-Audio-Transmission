#pragma warning disable CS8618 // 退出建構函式時，不可為 Null 的欄位必須包含非 Null 值。請考慮宣告為可為 Null。

using Discord_Audio_Transmission.NetworkChat;
using Discord_Audio_Transmission.Utils;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

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
        Regex ipAddrRegex = new Regex(@"^(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$");

        public MainWindow()
        {
            InitializeComponent();

            if (WaveIn.DeviceCount <= 0)
            {
                MessageBox.Show(this, "沒有音效輸入設備\n請確認設備是否已插入或被停用", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                tab_Sender.Dispatcher.Invoke(() => { tab_Sender.IsEnabled = false; });
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

                cb_PlayDevice.Dispatcher.Invoke(new Action(() => { cb_PlayDevice.Items.Add("預設播放裝置"); }));

                foreach (var item in GetOutputAudioDevices())
                {
                    cb_PlayDevice.Dispatcher.Invoke(new Action(() => { cb_PlayDevice.Items.Add($"{item.Key}"); }));
                }

                cb_PlayDevice.Dispatcher.Invoke(new Action(() => { cb_PlayDevice.SelectedIndex = 0; }));
            }

            // use reflection to find all the codecs
            // NAudio Demo
            var codecs = ReflectionHelper.CreateAllInstancesOf<INetworkChatCodec>();
            PopulateCodecsCombo(codecs);
        }

        // NAudio Demo
        private void btn_StartSend_Click(object sender, RoutedEventArgs e)
        {
            if (!ipAddrRegex.IsMatch(txt_IP.Text))
            {
                MessageBox.Show(this, "IP格式錯誤", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int port;
            if (!int.TryParse(txt_Port.Text, out port))
            {
                port = FreeTcpPort();
                txt_Port.Dispatcher.Invoke(() => { txt_Port.Text = $"{port}"; });
            }

            try
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(txt_IP.Text), port);
                networkAudioSender = new NetworkAudioSender(((CodecComboItem)cb_Code.SelectedItem).Codec, recordDeviceNumber, new UdpAudioSender(endPoint));
                networkAudioSender.MaximumCalculated += ((sender, e) => audioVisualization_Sender.AddValue(e.MinSample, e.MaxSample));
                btn_StartSend.Dispatcher.Invoke(() => { btn_StartSend.IsEnabled = false; });
                btn_StopSend.Dispatcher.Invoke(() => { btn_StopSend.IsEnabled = true; });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);

                btn_StartSend.Dispatcher.Invoke(() => { btn_StartSend.IsEnabled = true; });
                btn_StopSend.Dispatcher.Invoke(() => { btn_StopSend.IsEnabled = false; });
                audioVisualization_Sender.Dispatcher.Invoke(() => { audioVisualization_Sender.Reset(); });
            }
        }

        private void btn_StopSend_Click(object sender, RoutedEventArgs e)
        {
            if (networkAudioSender != null)
            {
                networkAudioSender.MaximumCalculated -= ((sender, e) => audioVisualization_Sender.AddValue(e.MinSample, e.MaxSample));
                networkAudioSender.Dispose();
            }

            btn_StartSend.Dispatcher.Invoke(() => { btn_StartSend.IsEnabled = true; });
            btn_StopSend.Dispatcher.Invoke(() => { btn_StopSend.IsEnabled = false; });
            audioVisualization_Sender.Dispatcher.Invoke(() => { audioVisualization_Sender.Reset(); });
        }

        // NAudio Demo
        private void btn_StartPlayer_Click(object sender, RoutedEventArgs e)
        {
            int port;
            if (!int.TryParse(txt_Port.Text, out port))
            {
                port = FreeTcpPort();
                txt_Port.Dispatcher.Invoke(() => { txt_Port.Text = $"{port}"; });
            }
            try
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
                networkAudioPlayer = new NetworkAudioPlayer(((CodecComboItem)cb_Code.SelectedItem).Codec, playerDeviceNumber, new UdpAudioReceiver(endPoint));
                networkAudioPlayer.MaximumCalculated += ((sender, e) => audioVisualization_Player.AddValue(e.MinSample, e.MaxSample));
                btn_StartPlay.Dispatcher.Invoke(() => { btn_StartPlay.IsEnabled = false; });
                btn_StopPlay.Dispatcher.Invoke(() => { btn_StopPlay.IsEnabled = true; });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);

                btn_StartPlay.Dispatcher.Invoke(() => { btn_StartPlay.IsEnabled = true; });
                btn_StopPlay.Dispatcher.Invoke(() => { btn_StopPlay.IsEnabled = false; });
                audioVisualization_Player.Dispatcher.Invoke(() => { audioVisualization_Player.Reset(); });
            }
        }

        private void btn_StopPlayer_Click(object sender, RoutedEventArgs e)
        {
            if (networkAudioPlayer != null)
            {
                networkAudioPlayer.MaximumCalculated -= ((sender, e) => audioVisualization_Player.AddValue(e.MinSample, e.MaxSample));
                networkAudioPlayer.Dispose();
            }

            btn_StartPlay.Dispatcher.Invoke(() => { btn_StartPlay.IsEnabled = true; });
            btn_StopPlay.Dispatcher.Invoke(() => { btn_StopPlay.IsEnabled = false; });
            audioVisualization_Player.Dispatcher.Invoke(() => { audioVisualization_Player.Reset(); });
        }

        private void cb_Device_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            recordDeviceNumber = cb_RecordDevice.SelectedIndex - 1;
            playerDeviceNumber = cb_PlayDevice.SelectedIndex - 1;
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
