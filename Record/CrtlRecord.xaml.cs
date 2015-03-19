using Microsoft.Win32;
using NAudio.CoreAudioApi;
using NAudio.MediaFoundation;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Record
{
    /// <summary>
    /// WinRecord.xaml 的交互逻辑
    /// </summary>
    public partial class CrtlRecord : UserControl
    {
        private SynchronizationContext _SynchronizationContext = null;
        private string _FileName = string.Empty;
        private MMDevice _MMDevice;
        private WasapiCapture _Capture;
        private DateTime _StartTime = DateTime.Now;
        private WaveFileWriter _Writer;
        private bool _IsRecording = false;

        public event Action Recorded;

        public CrtlRecord()
        {
            InitializeComponent();

            this._SynchronizationContext = SynchronizationContext.Current;
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            this._MMDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);

            this.btnRecord.Click += btnRecord_Click;
            this.btnStop.Click += btnStop_Click;
            this.Loaded += CrtlRecord_Loaded;
        }

        void CrtlRecord_Loaded(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(this);
            if (window != null)
                window.Closed += window_Closed;
        }

        void window_Closed(object sender, EventArgs e)
        {
            this.StopCapture();
        }

        public string FileName
        {
            get { return this._FileName; }
        }

        void btnRecord_Click(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.txtLoading.Visibility = System.Windows.Visibility.Visible;
            }), DispatcherPriority.Render, null);
            this.Dispatcher.Invoke(new Action(() =>
            {
                string dir = System.IO.Path.Combine(Environment.GetEnvironmentVariable("temp"), "ucdisk", "sound");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                this._FileName = System.IO.Path.Combine(dir, Guid.NewGuid().ToString() + ".wav");
                this.StartCapture();
            }), DispatcherPriority.ApplicationIdle, null);
        }

        void btnStop_Click(object sender, RoutedEventArgs e)
        {
            this.StopCapture();
        }

        public void StartCapture()
        {
            if (!this._IsRecording)
            {
                if (this._MMDevice != null)
                {
                    try
                    {
                        this._Capture = new WasapiCapture(this._MMDevice);
                        this._Capture.ShareMode = AudioClientShareMode.Shared;
                        this._Capture.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
                        this._Capture.RecordingStopped += _Capture_RecordingStopped;
                        this._Capture.DataAvailable += _Capture_DataAvailable;
                        this._Writer = new WaveFileWriter(this._FileName, this._Capture.WaveFormat);
                        this._Capture.StartRecording();
                        this._StartTime = DateTime.Now;
                        this._IsRecording = true;
                        this.btnStop.Visibility = System.Windows.Visibility.Visible;
                        this.btnRecord.Visibility = System.Windows.Visibility.Hidden;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    finally
                    {
                        this.txtLoading.Visibility = System.Windows.Visibility.Hidden;
                    }
                }
            }
        }

        void _Capture_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (this._Writer != null)
            {
                this._Writer.Write(e.Buffer, 0, e.BytesRecorded);
            }
            this._SynchronizationContext.Post(new SendOrPostCallback((obj) =>
            {
                this.prg.Value = this._MMDevice.AudioMeterInformation.MasterPeakValue;
                this.txtTime.Text = (DateTime.Now - this._StartTime).ToString(@"mm\:ss");
            }), null);
        }

        void _Capture_RecordingStopped(object sender, StoppedEventArgs e)
        {
            if (this._Writer != null)
            {
                try
                {
                    this._Writer.Dispose();
                }
                catch { }
            }
            this._Writer = null;
            if (this._Capture != null)
            {
                try
                {
                    this._Capture.RecordingStopped -= _Capture_RecordingStopped;
                    this._Capture.DataAvailable -= _Capture_DataAvailable;
                    this._Capture.Dispose();
                }
                catch { }
            }
            this._Capture = null;
            try
            {
                this.Mp3Encoding();
            }
            catch { }
            this.txtSave.Visibility = System.Windows.Visibility.Hidden;
            this.btnStop.Visibility = System.Windows.Visibility.Hidden;
            this.btnRecord.Visibility = System.Windows.Visibility.Visible;
            this._IsRecording = false;

            if (this.Recorded != null)
                this.Recorded();
        }

        public void StopCapture()
        {
            if (this._IsRecording)
            {
                this.txtSave.Visibility = System.Windows.Visibility.Visible;
                this._Capture.StopRecording();
                this.prg.Value = 0;
                this.txtTime.Text = "00:00";
            }
        }

        private void Mp3Encoding()
        {
            MediaType[] mediaTypes = MediaFoundationEncoder.GetOutputMediaTypes(AudioSubtypes.MFAudioFormat_MP3);
            if (mediaTypes != null && mediaTypes.Length > 0)
            {
                MediaType type = mediaTypes[0];
                using (MediaFoundationReader reader = new MediaFoundationReader(this._FileName))
                {
                    using (MediaFoundationEncoder encoder = new MediaFoundationEncoder(type))
                    {
                        encoder.Encode(this._FileName.Substring(0, this._FileName.Length - 4) + ".mp3", reader);
                        this._FileName = this._FileName.Substring(0, this._FileName.Length - 4) + ".mp3";
                    }
                }
            }
        }

        public static bool IsSupportRecord()
        {
            bool result = false;
            try
            {
                MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
                if (enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console) != null)
                    result = true;
            }
            catch
            { }
            return result;
        }
    }
}
