using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.Utils;

namespace RockBox
{
    class EqualizerBand
    {
        public float Frequency { get; set; }
        public float Gain { get; set; }
        public float Bandwidth { get; set; }
    }


    class Equalizer : ISampleProvider
    {
        private readonly ISampleProvider sourceProvider;
        private readonly EqualizerBand[] bands;
        private readonly BiQuadFilter[,] filters;
        private readonly int channels;
        private readonly int bandCount;
        private bool updated;

        public Equalizer(ISampleProvider sourceProvider, EqualizerBand[] bands)
        {
            this.sourceProvider = sourceProvider;
            this.bands = bands;
            channels = sourceProvider.WaveFormat.Channels;
            bandCount = bands.Length;
            filters = new BiQuadFilter[channels, bands.Length];
            CreateFilters();
        }

        private void CreateFilters()
        {
            for (int bandIndex = 0; bandIndex < bandCount; bandIndex++)
            {
                var band = bands[bandIndex];
                for (int n = 0; n < channels; n++)
                {
                    if (filters[n, bandIndex] == null)
                        filters[n, bandIndex] = BiQuadFilter.PeakingEQ(sourceProvider.WaveFormat.SampleRate, band.Frequency, band.Bandwidth, band.Gain);
                    else
                        filters[n, bandIndex].SetPeakingEq(sourceProvider.WaveFormat.SampleRate, band.Frequency, band.Bandwidth, band.Gain);
                }
            }
        }

        public void Update()
        {
            updated = true;
            CreateFilters();
        }

        public WaveFormat WaveFormat { get { return sourceProvider.WaveFormat; } }

        /// <summary>
        /// Sample
        /// </summary>
        public event EventHandler<SampleEventArgs> Sample;

        // reuse the same object every time to avoid making lots of work for the garbage collector
        private SampleEventArgs sampleEventArgs = new SampleEventArgs(0, 0);

        /// <summary>
        /// Raise the sample event (no check for null because it has already been done)
        /// </summary>
        private void RaiseSample(float left, float right)
        {
            sampleEventArgs.Left = left;
            sampleEventArgs.Right = right;
            Sample(this, sampleEventArgs);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = sourceProvider.Read(buffer, offset, count);


            if (updated)
            {
                CreateFilters();
                updated = false;
            }
            float left;
            float right;
            int i = 0;

            for (int n = 0; n < samplesRead; n++)
            {
                int ch = n % channels;

                for (int band = 0; band < bandCount; band++)
                {
                    buffer[offset + n] = filters[ch, band].Transform(buffer[offset + n]);
                }

                if (Sample != null)
                {
                    if (i % 2 == 0 && i < samplesRead - 2)
                    {
                        left = buffer[i];
                        right = buffer[++i];
                        RaiseSample(left, right);
                    }
                }

            }

            return samplesRead;
        }
    }

}

namespace RockBox.SampleConverters
{
    /// <summary>
    /// Sample provider interface to make WaveChannel32 extensible
    /// Still a bit ugly, hence internal at the moment - and might even make these into
    /// bit depth converting WaveProviders
    /// </summary>
    interface ISampleChunkConverter
    {
        bool Supports(WaveFormat format);
        void LoadNextChunk(IWaveProvider sourceProvider, int samplePairsRequired);
        bool GetNextSample(out float sampleLeft, out float sampleRight);
    }

    class Mono16SampleChunkConverter : ISampleChunkConverter
    {
        private int sourceSample;
        private byte[] sourceBuffer;
        private WaveBuffer sourceWaveBuffer;
        private int sourceSamples;

        public bool Supports(WaveFormat waveFormat)
        {
            return waveFormat.Encoding == WaveFormatEncoding.Pcm &&
                waveFormat.BitsPerSample == 16 &&
                waveFormat.Channels == 1;
        }

        public void LoadNextChunk(IWaveProvider source, int samplePairsRequired)
        {
            int sourceBytesRequired = samplePairsRequired * 2;
            sourceSample = 0;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, sourceBytesRequired);
            sourceWaveBuffer = new WaveBuffer(sourceBuffer);
            sourceSamples = source.Read(sourceBuffer, 0, sourceBytesRequired) / 2;
        }

        public bool GetNextSample(out float sampleLeft, out float sampleRight)
        {
            if (sourceSample < sourceSamples)
            {
                sampleLeft = sourceWaveBuffer.ShortBuffer[sourceSample++] / 32768.0f;
                sampleRight = sampleLeft;
                return true;
            }
            else
            {
                sampleLeft = 0.0f;
                sampleRight = 0.0f;
                return false;
            }
        }
    }

    class Mono24SampleChunkConverter : ISampleChunkConverter
    {
        private int offset;
        private byte[] sourceBuffer;
        private int sourceBytes;

        public bool Supports(WaveFormat waveFormat)
        {
            return waveFormat.Encoding == WaveFormatEncoding.Pcm &&
                waveFormat.BitsPerSample == 24 &&
                waveFormat.Channels == 1;
        }

        public void LoadNextChunk(IWaveProvider source, int samplePairsRequired)
        {
            int sourceBytesRequired = samplePairsRequired * 3;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, sourceBytesRequired);
            sourceBytes = source.Read(sourceBuffer, 0, sourceBytesRequired);
            offset = 0;
        }

        public bool GetNextSample(out float sampleLeft, out float sampleRight)
        {
            if (offset < sourceBytes)
            {
                sampleLeft = (((sbyte)sourceBuffer[offset + 2] << 16) | (sourceBuffer[offset + 1] << 8) | sourceBuffer[offset]) / 8388608f;
                offset += 3;
                sampleRight = sampleLeft;
                return true;
            }
            else
            {
                sampleLeft = 0.0f;
                sampleRight = 0.0f;
                return false;
            }
        }
    }

    class Mono8SampleChunkConverter : ISampleChunkConverter
    {
        private int offset;
        private byte[] sourceBuffer;
        private int sourceBytes;

        public bool Supports(WaveFormat waveFormat)
        {
            return waveFormat.Encoding == WaveFormatEncoding.Pcm &&
                waveFormat.BitsPerSample == 8 &&
                waveFormat.Channels == 1;
        }

        public void LoadNextChunk(IWaveProvider source, int samplePairsRequired)
        {
            int sourceBytesRequired = samplePairsRequired;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, sourceBytesRequired);
            sourceBytes = source.Read(sourceBuffer, 0, sourceBytesRequired);
            offset = 0;
        }

        public bool GetNextSample(out float sampleLeft, out float sampleRight)
        {
            if (offset < sourceBytes)
            {
                sampleLeft = sourceBuffer[offset] / 256f;
                offset++;
                sampleRight = sampleLeft;
                return true;
            }
            else
            {
                sampleLeft = 0.0f;
                sampleRight = 0.0f;
                return false;
            }
        }
    }

    class MonoFloatSampleChunkConverter : ISampleChunkConverter
    {
        private int sourceSample;
        private byte[] sourceBuffer;
        private WaveBuffer sourceWaveBuffer;
        private int sourceSamples;

        public bool Supports(WaveFormat waveFormat)
        {
            return waveFormat.Encoding == WaveFormatEncoding.IeeeFloat &&
                waveFormat.Channels == 1;
        }

        public void LoadNextChunk(IWaveProvider source, int samplePairsRequired)
        {
            int sourceBytesRequired = samplePairsRequired * 4;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, sourceBytesRequired);
            sourceWaveBuffer = new WaveBuffer(sourceBuffer);
            sourceSamples = source.Read(sourceBuffer, 0, sourceBytesRequired) / 4;
            sourceSample = 0;
        }

        public bool GetNextSample(out float sampleLeft, out float sampleRight)
        {
            if (sourceSample < sourceSamples)
            {
                sampleLeft = sourceWaveBuffer.FloatBuffer[sourceSample++];
                sampleRight = sampleLeft;
                return true;
            }
            else
            {
                sampleLeft = 0.0f;
                sampleRight = 0.0f;
                return false;
            }
        }
    }

    class Stereo16SampleChunkConverter : ISampleChunkConverter
    {
        private int sourceSample;
        private byte[] sourceBuffer;
        private WaveBuffer sourceWaveBuffer;
        private int sourceSamples;

        public bool Supports(WaveFormat waveFormat)
        {
            return waveFormat.Encoding == WaveFormatEncoding.Pcm &&
                waveFormat.BitsPerSample == 16 &&
                waveFormat.Channels == 2;
        }

        public void LoadNextChunk(IWaveProvider source, int samplePairsRequired)
        {
            int sourceBytesRequired = samplePairsRequired * 4;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, sourceBytesRequired);
            sourceWaveBuffer = new WaveBuffer(sourceBuffer);
            sourceSamples = source.Read(sourceBuffer, 0, sourceBytesRequired) / 2;
            sourceSample = 0;
        }

        public bool GetNextSample(out float sampleLeft, out float sampleRight)
        {
            if (sourceSample < sourceSamples)
            {
                sampleLeft = sourceWaveBuffer.ShortBuffer[sourceSample++] / 32768.0f;
                sampleRight = sourceWaveBuffer.ShortBuffer[sourceSample++] / 32768.0f;
                return true;
            }
            else
            {
                sampleLeft = 0.0f;
                sampleRight = 0.0f;
                return false;
            }
        }
    }

    class Stereo24SampleChunkConverter : ISampleChunkConverter
    {
        private int offset;
        private byte[] sourceBuffer;
        private int sourceBytes;

        public bool Supports(WaveFormat waveFormat)
        {
            return waveFormat.Encoding == WaveFormatEncoding.Pcm &&
                waveFormat.BitsPerSample == 24 &&
                waveFormat.Channels == 2;
        }


        public void LoadNextChunk(IWaveProvider source, int samplePairsRequired)
        {
            int sourceBytesRequired = samplePairsRequired * 6;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, sourceBytesRequired);
            sourceBytes = source.Read(sourceBuffer, 0, sourceBytesRequired);
            offset = 0;
        }

        public bool GetNextSample(out float sampleLeft, out float sampleRight)
        {
            if (offset < sourceBytes)
            {
                sampleLeft = (((sbyte)sourceBuffer[offset + 2] << 16) | (sourceBuffer[offset + 1] << 8) | sourceBuffer[offset]) / 8388608f;
                offset += 3;
                sampleRight = (((sbyte)sourceBuffer[offset + 2] << 16) | (sourceBuffer[offset + 1] << 8) | sourceBuffer[offset]) / 8388608f;
                offset += 3;
                return true;
            }
            else
            {
                sampleLeft = 0.0f;
                sampleRight = 0.0f;
                return false;
            }
        }

    }

    class Stereo8SampleChunkConverter : ISampleChunkConverter
    {
        private int offset;
        private byte[] sourceBuffer;
        private int sourceBytes;

        public bool Supports(WaveFormat waveFormat)
        {
            return waveFormat.Encoding == WaveFormatEncoding.Pcm &&
                waveFormat.BitsPerSample == 8 &&
                waveFormat.Channels == 2;
        }

        public void LoadNextChunk(IWaveProvider source, int samplePairsRequired)
        {
            int sourceBytesRequired = samplePairsRequired * 2;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, sourceBytesRequired);
            sourceBytes = source.Read(sourceBuffer, 0, sourceBytesRequired);
            offset = 0;
        }

        public bool GetNextSample(out float sampleLeft, out float sampleRight)
        {
            if (offset < sourceBytes)
            {
                sampleLeft = sourceBuffer[offset++] / 256f;
                sampleRight = sourceBuffer[offset++] / 256f;
                return true;
            }
            else
            {
                sampleLeft = 0.0f;
                sampleRight = 0.0f;
                return false;
            }
        }
    }

    class StereoFloatSampleChunkConverter : ISampleChunkConverter
    {
        private int sourceSample;
        private byte[] sourceBuffer;
        private WaveBuffer sourceWaveBuffer;
        private int sourceSamples;

        public bool Supports(WaveFormat waveFormat)
        {
            return waveFormat.Encoding == WaveFormatEncoding.IeeeFloat &&
                waveFormat.Channels == 2;
        }

        public void LoadNextChunk(IWaveProvider source, int samplePairsRequired)
        {
            int sourceBytesRequired = samplePairsRequired * 8;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, sourceBytesRequired);
            sourceWaveBuffer = new WaveBuffer(sourceBuffer);
            sourceSamples = source.Read(sourceBuffer, 0, sourceBytesRequired) / 4;
            sourceSample = 0;
        }

        public bool GetNextSample(out float sampleLeft, out float sampleRight)
        {
            if (sourceSample < sourceSamples)
            {
                sampleLeft = sourceWaveBuffer.FloatBuffer[sourceSample++];
                sampleRight = sourceWaveBuffer.FloatBuffer[sourceSample++];
                return true;
            }
            else
            {
                sampleLeft = 0.0f;
                sampleRight = 0.0f;
                return false;
            }
        }
    }
}


namespace NAudio.Wave
{
    /// <summary>
    /// AudioFileReader simplifies opening an audio file in NAudio
    /// Simply pass in the filename, and it will attempt to open the
    /// file and set up a conversion path that turns into PCM IEEE float.
    /// ACM codecs will be used for conversion.
    /// It provides a volume property and implements both WaveStream and
    /// ISampleProvider, making it possibly the only stage in your audio
    /// pipeline necessary for simple playback scenarios
    /// </summary>
    public class AudioFileReaderRB : WaveStream, ISampleProvider
    {
        private float pan;
        private string fileName;
        private WaveStream readerStream; // the waveStream which we will use for all positioning
        private readonly SampleChannel sampleChannel; // sample provider that gives us most stuff we need
        private readonly int destBytesPerSample;
        private readonly int sourceBytesPerSample;
        private readonly long length;
        private readonly object lockObject;
        private readonly RockBox.SampleConverters.ISampleChunkConverter sampleProvider;
        //WARNFIX
        //private readonly WaveFormat waveFormat;

        /// <summary>
        /// Initializes a new instance of AudioFileReader
        /// </summary>
        /// <param name="fileName">The file to open</param>
        public AudioFileReaderRB(string fileName)
        {
            lockObject = new object();
            this.fileName = fileName;
            CreateReaderStream(fileName);
            sourceBytesPerSample = (readerStream.WaveFormat.BitsPerSample / 8) * readerStream.WaveFormat.Channels;
            sampleChannel = new SampleChannel(readerStream, false);
            destBytesPerSample = 4 * sampleChannel.WaveFormat.Channels;
            length = SourceToDest(readerStream.Length);
            PadWithZeroes = true;
            pan = 0;

            var providers = new RockBox.SampleConverters.ISampleChunkConverter[]
            {
                new RockBox.SampleConverters.Mono8SampleChunkConverter(),
                new RockBox.SampleConverters.Stereo8SampleChunkConverter(),
                new RockBox.SampleConverters.Mono16SampleChunkConverter(),
                new RockBox.SampleConverters.Stereo16SampleChunkConverter(),
                new RockBox.SampleConverters.Mono24SampleChunkConverter(),
                new RockBox.SampleConverters.Stereo24SampleChunkConverter(),
                new RockBox.SampleConverters.MonoFloatSampleChunkConverter(),
                new RockBox.SampleConverters.StereoFloatSampleChunkConverter(),
            };
            foreach (var provider in providers)
            {
                if (provider.Supports(readerStream.WaveFormat))
                {
                    this.sampleProvider = provider;
                    break;
                }
            }

            if (this.sampleProvider == null)
            {
                throw new ArgumentException("Unsupported sourceStream format");
            }

        }

        /// <summary>
        /// Creates the reader stream, supporting all filetypes in the core NAudio library,
        /// and ensuring we are in PCM format
        /// </summary>
        /// <param name="fileName">File Name</param>
        private void CreateReaderStream(string fileName)
        {
            if (fileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                readerStream = new WaveFileReader(fileName);
                if (readerStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm && readerStream.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                {
                    readerStream = WaveFormatConversionStream.CreatePcmStream(readerStream);
                    readerStream = new BlockAlignReductionStream(readerStream);
                }
            }
            else if (fileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
            {
                readerStream = new Mp3FileReader(fileName);
            }
            else if (fileName.EndsWith(".aiff"))
            {
                readerStream = new AiffFileReader(fileName);
            }
            else
            {
                // fall back to media foundation reader, see if that can play it
                readerStream = new MediaFoundationReader(fileName);
            }
        }

        /// <summary>
        /// WaveFormat of this stream
        /// </summary>
        public override WaveFormat WaveFormat
        {
            get { return sampleChannel.WaveFormat; }
        }

        public WaveStream ReaderStream
        {
            get { return this.readerStream; }
        }

        /// <summary>
        /// Pan of this channel (from -1 to 1)
        /// </summary>
        public float Pan
        {
            get { return pan; }
            set { pan = value; }
        }

        /// <summary>
        /// Length of this stream (in bytes)
        /// </summary>
        public override long Length
        {
            get { return length; }
        }

        /// <summary>
        /// Position of this stream (in bytes)
        /// </summary>
        public override long Position
        {
            get { return SourceToDest(readerStream.Position); }
            set { lock (lockObject) { readerStream.Position = DestToSource(value); } }
        }

        /// <summary>
        /// Reads audio from this sample provider
        /// </summary>
        /// <param name="buffer">Sample buffer</param>
        /// <param name="offset">Offset into sample buffer</param>
        /// <param name="count">Number of samples required</param>
        /// <returns>Number of samples read</returns>
        ///
        public int Read(float[] buffer, int offset, int count)
        {
            lock (lockObject)
            {
                return sampleChannel.Read(buffer, offset, count);
            }
        }


        /// <summary>
        /// Reads from this wave stream
        /// </summary>
        /// <param name="buffer">Audio buffer</param>
        /// <param name="offset">Offset into buffer</param>
        /// <param name="count">Number of bytes required</param>
        /// <returns>Number of bytes read</returns>
        public override int Read(byte[] destBuffer, int offset, int numBytes)
        {
            /*
            var waveBuffer = new WaveBuffer(buffer);
            int samplesRequired = count / 4;
            int samplesRead = Read(waveBuffer.FloatBuffer, offset / 4, samplesRequired);
            return samplesRead * 4;
             */

            lock (lockObject)
            {
                int bytesWritten = 0;
                WaveBuffer destWaveBuffer = new WaveBuffer(destBuffer);

                // 1. fill with silence
                if (this.Position < 0)
                {
                    bytesWritten = (int)Math.Min(numBytes, 0 - this.Position);
                    for (int n = 0; n < bytesWritten; n++)
                        destBuffer[n + offset] = 0;
                }
                if (bytesWritten < numBytes)
                {
                    this.sampleProvider.LoadNextChunk(readerStream, (numBytes - bytesWritten) / 8);
                    float left, right;

                    int outIndex = (offset / 4) + bytesWritten / 4;
                    while (this.sampleProvider.GetNextSample(out left, out right) && bytesWritten < numBytes)
                    {
                        // implement better panning laws. 
                        left = (pan <= 0) ? left : (left * (1 - pan) / 2.0f);
                        right = (pan >= 0) ? right : (right * (pan + 1) / 2.0f);
                        left *= this.Volume;
                        right *= this.Volume;
                        destWaveBuffer.FloatBuffer[outIndex++] = left;
                        destWaveBuffer.FloatBuffer[outIndex++] = right;
                        bytesWritten += 8;
                        if (Sample != null) RaiseSample(left, right);
                    }
                }
                // 3. Fill out with zeroes
                if (PadWithZeroes && bytesWritten < numBytes)
                {
                    Array.Clear(destBuffer, offset + bytesWritten, numBytes - bytesWritten);
                    bytesWritten = numBytes;
                }
                this.Position += bytesWritten;
                return bytesWritten;
            }

        }

        /// <summary>
        /// If true, Read always returns the number of bytes requested
        /// </summary>
        public bool PadWithZeroes { get; set; }


        /// <summary>
        /// Gets or Sets the Volume of this AudioFileReader. 1.0f is full volume
        /// </summary>
        public float Volume
        {
            get { return sampleChannel.Volume; }
            set { sampleChannel.Volume = value; }
        }

        /// <summary>
        /// Helper to convert source to dest bytes
        /// </summary>
        private long SourceToDest(long sourceBytes)
        {
            return destBytesPerSample * (sourceBytes / sourceBytesPerSample);
        }

        /// <summary>
        /// Helper to convert dest to source bytes
        /// </summary>
        private long DestToSource(long destBytes)
        {
            return sourceBytesPerSample * (destBytes / destBytesPerSample);
        }

        /// <summary>
        /// Disposes this AudioFileReader
        /// </summary>
        /// <param name="disposing">True if called from Dispose</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                readerStream.Dispose();
                readerStream = null;
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Sample
        /// </summary>
        public event EventHandler<SampleEventArgs> Sample;

        // reuse the same object every time to avoid making lots of work for the garbage collector
        private SampleEventArgs sampleEventArgs = new SampleEventArgs(0, 0);

        /// <summary>
        /// Raise the sample event (no check for null because it has already been done)
        /// </summary>
        private void RaiseSample(float left, float right)
        {
            sampleEventArgs.Left = left;
            sampleEventArgs.Right = right;
            Sample(this, sampleEventArgs);
        }
    }
}


