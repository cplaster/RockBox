// (c) Copyright Jacob Johnston.
// This source is subject to Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Threading;
using NAudio.Wave;
using NAudio.Dsp;
using WPFSoundVisualizationLib;

namespace RockBox
{
    public class SampleAggregator
    {
        private float volumeLeftMaxValue;
        private float volumeLeftMinValue;
        private float volumeRightMaxValue;
        private float volumeRightMinValue;
        private Complex[] channelData;
        private int bufferSize;
        private int binaryExponentitation;
        private int channelDataPosition;

        public SampleAggregator(int bufferSize)
        {
            this.bufferSize = bufferSize;
            binaryExponentitation = (int)Math.Log(bufferSize, 2);
            channelData = new Complex[bufferSize];
        }

        public void Clear()
        {
            volumeLeftMaxValue = float.MinValue;
            volumeRightMaxValue = float.MinValue;
            volumeLeftMinValue = float.MaxValue;
            volumeRightMinValue = float.MaxValue;
            channelDataPosition = 0;
        }

        /// <summary>
        /// Add a sample value to the aggregator.
        /// </summary>
        /// <param name="value">The value of the sample.</param>
        public void Add(float leftValue, float rightValue)
        {
            if (channelDataPosition == 0)
            {
                volumeLeftMaxValue = float.MinValue;
                volumeRightMaxValue = float.MinValue;
                volumeLeftMinValue = float.MaxValue;
                volumeRightMinValue = float.MaxValue;
            }

            // Make stored channel data stereo by averaging left and right values.
            channelData[channelDataPosition].X = (leftValue + rightValue) / 2.0f;
            channelData[channelDataPosition].Y = 0;
            channelDataPosition++;

            volumeLeftMaxValue = Math.Max(volumeLeftMaxValue, leftValue);
            volumeLeftMinValue = Math.Min(volumeLeftMinValue, leftValue);
            volumeRightMaxValue = Math.Max(volumeRightMaxValue, rightValue);
            volumeRightMinValue = Math.Min(volumeRightMinValue, rightValue);

            if (channelDataPosition >= channelData.Length)
            {
                channelDataPosition = 0;
            }
        }

        /// <summary>
        /// Performs an FFT calculation on the channel data upon request.
        /// </summary>
        /// <param name="fftBuffer">A buffer where the FFT data will be stored.</param>
        public void GetFFTResults(float[] fftBuffer)
        {
            Complex[] channelDataClone = new Complex[bufferSize];
            channelData.CopyTo(channelDataClone, 0);
            FastFourierTransform.FFT(true, binaryExponentitation, channelDataClone);
            for (int i = 0; i < channelDataClone.Length / 2; i++)
            {
                // Calculate actual intensities for the FFT results.
                fftBuffer[i] = (float)Math.Sqrt(channelDataClone[i].X * channelDataClone[i].X + channelDataClone[i].Y * channelDataClone[i].Y);
            }
        }

        public float LeftMaxVolume
        {
            get { return volumeLeftMaxValue; }
        }

        public float LeftMinVolume
        {
            get { return volumeLeftMinValue; }
        }

        public float RightMaxVolume
        {
            get { return volumeRightMaxValue; }
        }

        public float RightMinVolume
        {
            get { return volumeRightMinValue; }
        }
    }

    class NAudioEngine : INotifyPropertyChanged, ISpectrumPlayer, IWaveformPlayer, IDisposable
    {
        #region Fields
        private long sampleReset = 0;
        private static NAudioEngine instance;
        private readonly EqualizerBand[] bands;
        private readonly DispatcherTimer positionTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle);
        private readonly BackgroundWorker waveformGenerateWorker = new BackgroundWorker();
        private readonly int fftDataSize = (int)FFTDataSize.FFT2048;
        private bool disposed;
        private bool canPlay;
        private bool canPause;
        private bool canStop;
        private bool isPlaying;
        private bool inChannelTimerUpdate;
        private double channelLength;
        private double channelPosition;
        private bool inChannelSet;
        private WaveOutEvent waveOutDevice;
        private WaveStream activeStream;
        private AudioFileReaderRB inputStream;
        private SampleAggregator sampleAggregator;
        private SampleAggregator waveformAggregator;
        private string pendingWaveformPath;
        private float[] fullLevelData;
        private float[] waveformData;
        private TagLib.File fileTag;
        private TimeSpan repeatStart;
        private TimeSpan repeatStop;
        private bool inRepeatSet;
        #endregion

        #region Constants
        private const int waveformCompressedPointCount = 2000;
        private const int repeatThreshold = 200;
        #endregion

        #region Singleton Pattern
        public static NAudioEngine Instance
        {
            get
            {
                if (instance == null)
                    instance = new NAudioEngine();
                return instance;
            }
        }
        #endregion

        #region Constructor
        private NAudioEngine()
        {
            positionTimer.Interval = TimeSpan.FromMilliseconds(50);
            positionTimer.Tick += positionTimer_Tick;

            waveformGenerateWorker.DoWork += waveformGenerateWorker_DoWork;
            waveformGenerateWorker.RunWorkerCompleted += waveformGenerateWorker_RunWorkerCompleted;
            waveformGenerateWorker.WorkerSupportsCancellation = true;

            bands = new EqualizerBand[]
                    {
                        new EqualizerBand {Bandwidth = 0.8f, Frequency = 100, Gain = 0},
                        new EqualizerBand {Bandwidth = 0.8f, Frequency = 200, Gain = 0},
                        new EqualizerBand {Bandwidth = 0.8f, Frequency = 400, Gain = 0},
                        new EqualizerBand {Bandwidth = 0.8f, Frequency = 800, Gain = 0},
                        new EqualizerBand {Bandwidth = 0.8f, Frequency = 1200, Gain = 0},
                        new EqualizerBand {Bandwidth = 0.8f, Frequency = 2400, Gain = 0},
                        new EqualizerBand {Bandwidth = 0.8f, Frequency = 4800, Gain = 0},
                        new EqualizerBand {Bandwidth = 0.8f, Frequency = 9600, Gain = 0},
                    };
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    StopAndCloseStream();
                }

                disposed = true;
            }
        }
        #endregion

        #region ISpectrumPlayer
        public bool GetFFTData(float[] fftDataBuffer)
        {
            sampleAggregator.GetFFTResults(fftDataBuffer);
            return isPlaying;
        }

        public int GetFFTFrequencyIndex(int frequency)
        {
            double maxFrequency;
            if (ActiveStream != null)
                maxFrequency = ActiveStream.WaveFormat.SampleRate / 2.0d;
            else
                maxFrequency = 22050; // Assume a default 44.1 kHz sample rate.
            return (int)((frequency / maxFrequency) * (fftDataSize / 2));
        }
        #endregion

        #region IWaveformPlayer
        public TimeSpan SelectionBegin
        {
            get { return repeatStart; }
            set
            {
                if (!inRepeatSet)
                {
                    inRepeatSet = true;
                    TimeSpan oldValue = repeatStart;
                    repeatStart = value;
                    if (oldValue != repeatStart)
                        NotifyPropertyChanged("SelectionBegin");
                    inRepeatSet = false;
                }
            }
        }

        public TimeSpan SelectionEnd
        {
            get { return repeatStop; }
            set
            {
                if (!inChannelSet)
                {
                    inRepeatSet = true;
                    TimeSpan oldValue = repeatStop;
                    repeatStop = value;
                    if (oldValue != repeatStop)
                        NotifyPropertyChanged("SelectionEnd");
                    inRepeatSet = false;
                }
            }
        }

        public float[] WaveformData
        {
            get { return waveformData; }
            protected set
            {
                float[] oldValue = waveformData;
                waveformData = value;
                if (oldValue != waveformData)
                    NotifyPropertyChanged("WaveformData");
            }
        }

        public double ChannelLength
        {
            get { return channelLength; }
            protected set
            {
                double oldValue = channelLength;
                channelLength = value;
                if (oldValue != channelLength)
                    NotifyPropertyChanged("ChannelLength");
            }
        }

        public double ChannelPosition
        {
            get { return channelPosition; }
            set
            {
                if (!inChannelSet)
                {
                    inChannelSet = true; // Avoid recursion
                    double oldValue = channelPosition;
                    double position = Math.Max(0, Math.Min(value, ChannelLength));
                    if (!inChannelTimerUpdate && ActiveStream != null)
                        ActiveStream.Position = (long)((position / ActiveStream.TotalTime.TotalSeconds) * ActiveStream.Length);
                    channelPosition = position;
                    if (oldValue != channelPosition)
                        NotifyPropertyChanged("ChannelPosition");
                    inChannelSet = false;
                }
            }
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
        #endregion

        #region Waveform Generation
        private class WaveformGenerationParams
        {
            public WaveformGenerationParams(int points, string path)
            {
                Points = points;
                Path = path;
            }

            public int Points { get; protected set; }
            public string Path { get; protected set; }
        }

        private void GenerateWaveformData(string path)
        {
            if (waveformGenerateWorker.IsBusy)
            {
                pendingWaveformPath = path;
                waveformGenerateWorker.CancelAsync();
                return;
            }

            if (!waveformGenerateWorker.IsBusy && waveformCompressedPointCount != 0)
                waveformGenerateWorker.RunWorkerAsync(new WaveformGenerationParams(waveformCompressedPointCount, path));
        }

        private void waveformGenerateWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                if (!waveformGenerateWorker.IsBusy && waveformCompressedPointCount != 0)
                    waveformGenerateWorker.RunWorkerAsync(new WaveformGenerationParams(waveformCompressedPointCount, pendingWaveformPath));
            }
        }

        private void waveformGenerateWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            WaveformGenerationParams waveformParams = e.Argument as WaveformGenerationParams;
            Mp3FileReader waveformMp3Stream = new Mp3FileReader(waveformParams.Path);
            WaveChannel32 waveformInputStream = new WaveChannel32(waveformMp3Stream);
            waveformInputStream.Sample += waveStream_Sample;

            int frameLength = fftDataSize;
            int frameCount = (int)((double)waveformInputStream.Length / (double)frameLength);
            int waveformLength = frameCount * 2;
            byte[] readBuffer = new byte[frameLength];
            waveformAggregator = new SampleAggregator(frameLength);

            float maxLeftPointLevel = float.MinValue;
            float maxRightPointLevel = float.MinValue;
            int currentPointIndex = 0;
            float[] waveformCompressedPoints = new float[waveformParams.Points];
            List<float> waveformData = new List<float>();
            List<int> waveMaxPointIndexes = new List<int>();

            for (int i = 1; i <= waveformParams.Points; i++)
            {
                waveMaxPointIndexes.Add((int)Math.Round(waveformLength * ((double)i / (double)waveformParams.Points), 0));
            }
            int readCount = 0;
            while (currentPointIndex * 2 < waveformParams.Points)
            {
                waveformInputStream.Read(readBuffer, 0, readBuffer.Length);

                waveformData.Add(waveformAggregator.LeftMaxVolume);
                waveformData.Add(waveformAggregator.RightMaxVolume);

                if (waveformAggregator.LeftMaxVolume > maxLeftPointLevel)
                    maxLeftPointLevel = waveformAggregator.LeftMaxVolume;
                if (waveformAggregator.RightMaxVolume > maxRightPointLevel)
                    maxRightPointLevel = waveformAggregator.RightMaxVolume;

                if (readCount > waveMaxPointIndexes[currentPointIndex])
                {
                    waveformCompressedPoints[(currentPointIndex * 2)] = maxLeftPointLevel;
                    waveformCompressedPoints[(currentPointIndex * 2) + 1] = maxRightPointLevel;
                    maxLeftPointLevel = float.MinValue;
                    maxRightPointLevel = float.MinValue;
                    currentPointIndex++;
                }
                if (readCount % 3000 == 0)
                {
                    float[] clonedData = (float[])waveformCompressedPoints.Clone();
                    App.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        WaveformData = clonedData;
                    }));
                }

                if (waveformGenerateWorker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                readCount++;
            }

            float[] finalClonedData = (float[])waveformCompressedPoints.Clone();
            App.Current.Dispatcher.Invoke(new Action(() =>
            {
                fullLevelData = waveformData.ToArray();
                WaveformData = finalClonedData;
            }));
            waveformInputStream.Close();
            waveformInputStream.Dispose();
            waveformInputStream = null;
            waveformMp3Stream.Close();
            waveformMp3Stream.Dispose();
            waveformMp3Stream = null;
        }
        #endregion

        #region Private Utility Methods
        private void StopAndCloseStream()
        {
            if (waveOutDevice != null)
            {
                waveOutDevice.Stop();
            }
            if (activeStream != null)
            {
                inputStream.Close();
                inputStream = null;
                ActiveStream.Close();
                ActiveStream = null;
            }
            if (waveOutDevice != null)
            {
                waveOutDevice.Dispose();
                waveOutDevice = null;
            }
        }
        #endregion

        #region Public Methods
        public void Stop()
        {
            if (waveOutDevice != null)
            {
                waveOutDevice.Stop();
            }
            IsPlaying = false;
            CanStop = false;
            CanPlay = true;
            CanPause = false;
        }

        public void Pause()
        {
            if (IsPlaying && CanPause)
            {
                waveOutDevice.Pause();
                IsPlaying = false;
                CanPlay = true;
                CanPause = false;
                return;
            }

            if (!IsPlaying && !CanPause)
            {
                this.Play();
                return;
            }

        }

        public void Play()
        {
            if (CanPlay)
            {
                if (waveOutDevice != null)
                {
                    waveOutDevice.Play();
                    waveOutDevice.PlaybackStopped += waveOutDevice_PlaybackStopped;
                    IsPlaying = true;
                    CanPause = true;
                    CanPlay = false;
                    CanStop = true;
                }
            }
        }

        public void OpenFile(string path)
        {
            Stop();

            if (ActiveStream != null)
            {
                sampleReset = 0;
                SelectionBegin = TimeSpan.Zero;
                SelectionEnd = TimeSpan.Zero;
                ChannelPosition = 0;
            }

            StopAndCloseStream();

            if (System.IO.File.Exists(path))
            {
                try
                {
                    waveOutDevice = new WaveOutEvent()
                    {
                        DesiredLatency = 100
                    };


                    this.inputStream = new AudioFileReaderRB(path);
                    this.ActiveStream = this.inputStream.ReaderStream;
                    sampleAggregator = new SampleAggregator(fftDataSize);

                    Equalizer eq = new Equalizer(inputStream, this.bands);
                    eq.Sample += eq_Sample;
                    waveOutDevice.Init(eq);
                    ChannelLength = inputStream.TotalTime.TotalSeconds;
                    FileTag = TagLib.File.Create(path);
                    //GenerateWaveformData(path);
                    CanPlay = true;
                }
                catch
                {
                    ActiveStream = null;
                    CanPlay = false;
                }
            }
        }

        #endregion

        #region Public Properties

        #region Equalizer Properties

        public float MinimumGain
        {
            get { return -30; }
        }

        public float MaximumGain
        {
            get { return 30; }
        }

        public float Band1
        {
            get { return bands[0].Gain; }
            set
            {
                if (bands[0].Gain != value)
                {
                    bands[0].Gain = value;
                    NotifyPropertyChanged("Band1");
                }
            }
        }

        public float Band2
        {
            get { return bands[1].Gain; }
            set
            {
                if (bands[1].Gain != value)
                {
                    bands[1].Gain = value;
                    NotifyPropertyChanged("Band2");
                }
            }
        }

        public float Band3
        {
            get { return bands[2].Gain; }
            set
            {
                if (bands[2].Gain != value)
                {
                    bands[2].Gain = value;
                    NotifyPropertyChanged("Band3");
                }
            }
        }

        public float Band4
        {
            get { return bands[3].Gain; }
            set
            {
                if (bands[3].Gain != value)
                {
                    bands[3].Gain = value;
                    NotifyPropertyChanged("Band4");
                }
            }
        }

        public float Band5
        {
            get { return bands[4].Gain; }
            set
            {
                if (bands[4].Gain != value)
                {
                    bands[4].Gain = value;
                    NotifyPropertyChanged("Band5");
                }
            }
        }

        public float Band6
        {
            get { return bands[5].Gain; }
            set
            {
                if (bands[5].Gain != value)
                {
                    bands[5].Gain = value;
                    NotifyPropertyChanged("Band6");
                }
            }
        }

        public float Band7
        {
            get { return bands[6].Gain; }
            set
            {
                if (bands[6].Gain != value)
                {
                    bands[6].Gain = value;
                    NotifyPropertyChanged("Band7");
                }
            }
        }

        public float Band8
        {
            get { return bands[7].Gain; }
            set
            {
                if (bands[7].Gain != value)
                {
                    bands[7].Gain = value;
                    NotifyPropertyChanged("Band7");
                }
            }
        }

        #endregion

        public TagLib.File FileTag
        {
            get { return fileTag; }
            set
            {
                TagLib.File oldValue = fileTag;
                fileTag = value;
                if (oldValue != fileTag)
                    NotifyPropertyChanged("FileTag");
            }
        }

        public WaveStream ActiveStream
        {
            get { return activeStream; }
            protected set
            {
                WaveStream oldValue = activeStream;
                activeStream = value;
                if (oldValue != activeStream)
                    NotifyPropertyChanged("ActiveStream");
            }
        }

        public bool CanPlay
        {
            get { return canPlay; }
            protected set
            {
                bool oldValue = canPlay;
                canPlay = value;
                if (oldValue != canPlay)
                    NotifyPropertyChanged("CanPlay");
            }
        }

        public bool CanPause
        {
            get { return canPause; }
            protected set
            {
                bool oldValue = canPause;
                canPause = value;
                if (oldValue != canPause)
                    NotifyPropertyChanged("CanPause");
            }
        }

        public bool CanStop
        {
            get { return canStop; }
            protected set
            {
                bool oldValue = canStop;
                canStop = value;
                if (oldValue != canStop)
                    NotifyPropertyChanged("CanStop");
            }
        }

        public bool IsPlaying
        {
            get { return isPlaying; }
            protected set
            {
                bool oldValue = isPlaying;
                isPlaying = value;
                if (oldValue != isPlaying)
                    NotifyPropertyChanged("IsPlaying");
                positionTimer.IsEnabled = value;
            }
        }

        public long SampleReset
        {
            get
            {
                return sampleReset;
            }
            set
            {
                sampleReset = value;
            }
        }

        public float Volume
        {
            get { return this.inputStream.Volume; }
            set
            {
                this.inputStream.Volume = value;
            }
        }
        #endregion

        #region Event Handlers

        void eq_Sample(object sender, SampleEventArgs e)
        {
            inputStream_Sample(sender, e);
        }

        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        void waveOutDevice_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            PlaybackStopped(sender, e);
        }

        private void inputStream_Sample(object sender, SampleEventArgs e)
        {
            sampleAggregator.Add(e.Left * 2.0f, e.Right * 2.0f);

            // I know, I know, magic numbers are bad, mmkay.

            if (ActiveStream != null)
            {
                if (sampleReset + 42000 < ActiveStream.Position)
                {
                    sampleReset = ActiveStream.Position;
                    sampleAggregator.Clear();
                }
            }
        }

        void waveStream_Sample(object sender, SampleEventArgs e)
        {
            waveformAggregator.Add(e.Left, e.Right);
        }

        void positionTimer_Tick(object sender, EventArgs e)
        {
            inChannelTimerUpdate = true;
            ChannelPosition = ((double)ActiveStream.Position / (double)ActiveStream.Length) * ActiveStream.TotalTime.TotalSeconds;
            inChannelTimerUpdate = false;


        }
        #endregion
    }
}

