using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Sandbox.CommonLib.ObjectBuilders.Audio;
using SysUtils.Utils;
using SharpDX.XAudio2;
using SharpDX.Multimedia;

namespace Sandbox.Engine.Audio
{
    class MyInMemoryWave : IDisposable
    { 
        SoundStream m_stream;
        WaveFormat m_waveFormat;
        AudioBuffer m_buffer;

        public WaveFormat WaveFormat { get { return m_waveFormat; } }
        public SoundStream Stream { get { return m_stream; } }
        public AudioBuffer Buffer { get { return m_buffer; } }

        public MyInMemoryWave(MyObjectBuilder_CueDefinition cue, string filename)
        {
            m_stream = new SoundStream(File.OpenRead(filename));
            m_waveFormat = m_stream.Format;
            m_buffer = new AudioBuffer
                            {
                                Stream = m_stream.ToDataStream(),
                                AudioBytes = (int)m_stream.Length,
                                Flags = BufferFlags.None
                            };

            if (cue.Loopable)
                m_buffer.LoopCount = AudioBuffer.LoopInfinite;

            m_stream.Close();
        }

        public void Dispose()
        {
            m_buffer.Stream.Dispose();
        }
    }

    class MyWaveBank : IDisposable
    {
        Dictionary<string, MyInMemoryWave> m_waves = new Dictionary<string, MyInMemoryWave>();

        public int Count { get { return m_waves.Count; } }

        public bool Add(MyObjectBuilder_CueDefinition cue, string waveFilename)
        {
            string filename = Path.Combine(MyAudio.AudioPath, waveFilename);
            bool result = File.Exists(filename);
            if (result)
            {
                MyInMemoryWave wave = new MyInMemoryWave(cue, filename);
                m_waves[waveFilename] = wave;
            }
            else
            {
                MySandboxGame.Log.WriteLine(string.Format("Unable to find audio file: {0}", filename), LoggingOptions.AUDIO);
            }
            return result;
        }

        public void Dispose()
        {
            foreach (var wave in m_waves)
            {
                wave.Value.Dispose();
            }
        }

        public MyInMemoryWave GetWave(string filename)
        {
            if (!m_waves.ContainsKey(filename))
                return null;

            return m_waves[filename];
        }

        public List<MyWaveFormat> GetWaveFormats()
        {
            List<MyWaveFormat> output = new List<MyWaveFormat>();
            foreach (var wave in m_waves)
            {
                MyWaveFormat myWaveFormat = new MyWaveFormat()
                {
                    Encoding = wave.Value.WaveFormat.Encoding,
                    Channels = wave.Value.WaveFormat.Channels,
                    SampleRate = wave.Value.WaveFormat.SampleRate,
                    WaveFormat = wave.Value.WaveFormat
                };
                if (!output.Contains(myWaveFormat))
                    output.Add(myWaveFormat);
            }
            return (output);
        }
    }
}
