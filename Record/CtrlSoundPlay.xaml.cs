using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Record
{
    /// <summary>
    /// CtrlSoundPlay.xaml 的交互逻辑
    /// </summary>
    public partial class CtrlSoundPlay : UserControl
    {
        private bool _IsPlaying = false;
        private string _FileName = string.Empty;
        private TimeSpan _Duration = TimeSpan.Zero;
        private DispatcherTimer _Timer = new DispatcherTimer();
        private WaveOutEvent _WavePlayer = null;
        private MediaFoundationReader _Reader = null;


        public CtrlSoundPlay()
        {
            InitializeComponent();

            this._Timer.Interval = TimeSpan.FromMilliseconds(500);
            this._Timer.Tick += timer_Tick;
            this.btnPlay.Click += btnPlay_Click;
        }

        public string FileName
        {
            get { return this._FileName; }
            set { this._FileName = value; }
        }

        void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (!this._IsPlaying)
            {
                this.Play();
            }
            else
            {
                this.Stop();
            }
        }

        void timer_Tick(object sender, EventArgs e)
        {
            double time = this._Reader.Position * this._Duration.TotalMilliseconds / this._Reader.Length;
            this.txtTime.Text = TimeSpan.FromMilliseconds(time).ToString(@"mm\′ss\″");
        }

        public void Init()
        {
            this._Duration = this.GetWavFileDuration();
            this.txtTime.Text = this._Duration.ToString(@"mm\′ss\″");

            Window window = Window.GetWindow(this);
            if (window != null)
            {
                window.Closed -= window_Closed;
                window.Closed += window_Closed;
            }
        }

        void window_Closed(object sender, EventArgs e)
        {
            this.Stop();
        }

        public void Play()
        {
            if (this._WavePlayer == null)
                this._WavePlayer = new WaveOutEvent();
            if (this._Reader == null)
                this._Reader = new MediaFoundationReader(this._FileName);
            this._WavePlayer.Init(this._Reader);
            this._WavePlayer.PlaybackStopped += wavePlayer_PlaybackStopped;
            this._WavePlayer.Play();
            this._Timer.Start();
            this._IsPlaying = true;

            this.play.Visibility = System.Windows.Visibility.Hidden;
            this.stop.Visibility = System.Windows.Visibility.Visible;
        }

        public void Stop()
        {
            this._WavePlayer.Stop();
        }

        void wavePlayer_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            this.txtTime.Text = this._Duration.ToString(@"mm\′ss\″");
            this._Timer.Stop();
            this._IsPlaying = false;

            this.play.Visibility = System.Windows.Visibility.Visible;
            this.stop.Visibility = System.Windows.Visibility.Hidden;

            if (this._Reader != null)
            {
                try
                {
                    this._Reader.Dispose();
                }
                catch { }
            }
            this._Reader = null;
            if (this._WavePlayer != null)
            {
                try
                {
                    this._WavePlayer.Dispose();
                }
                catch { }
            }
            this._WavePlayer = null;
        }

        public TimeSpan GetWavFileDuration()
        {
            TimeSpan result = TimeSpan.Zero;
            if (this._Reader == null)
                this._Reader = new MediaFoundationReader(this._FileName);
            result = _Reader.TotalTime;
            return result;
        }
    }
}
