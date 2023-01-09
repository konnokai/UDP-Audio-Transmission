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

namespace Discord_Audio_Transmission
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int deviceNumber = 0;

        public MainWindow()
        {
            if (WaveIn.DeviceCount <= 0)
            {
                MessageBox.Show(this, "沒有音效輸入設備\n請確認設備是否已插入或被停用", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);                
                Environment.Exit(1);
                return;
            }

            InitializeComponent();

            cb_RecordDevice.Dispatcher.Invoke(new Action(() => { cb_RecordDevice.Items.Add("預設錄音裝置"); }));

            foreach (var item in GetInputAudioDevices())
            {
                cb_RecordDevice.Dispatcher.Invoke(new Action(() => { cb_RecordDevice.Items.Add($"{item.Key}"); }));
            }

            cb_RecordDevice.Dispatcher.Invoke(new Action(() => { cb_RecordDevice.SelectedIndex = 0; }));

            // use reflection to find all the codecs
            // NAudio Demo
            var codecs = ReflectionHelper.CreateAllInstancesOf<INetworkChatCodec>();
            PopulateCodecsCombo(codecs);
        }

        private void RecorderOnDataAvailable(object? sender, WaveInEventArgs waveInEventArgs)
        {
        }

        // https://markheath.net/post/how-to-record-and-play-audio-at-same
        private void btn_Start_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btn_Stop_Click(object sender, RoutedEventArgs e)
        {
        }

        private void cb_RecordDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            deviceNumber = cb_RecordDevice.SelectedIndex - 1;
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

            foreach (MMDevice device in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.All))
            {
                if (device.State == DeviceState.NotPresent)
                    continue;

                if (!retVal.ContainsKey(device.FriendlyName))
                    retVal.Add("(" + (device.State != DeviceState.Active ? "未" : "已") + $"啟用) {device.FriendlyName}", device);
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
