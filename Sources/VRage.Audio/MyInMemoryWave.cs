using SharpDX.Multimedia;
using SharpDX.XAudio2;
using System;
using VRage.Data.Audio;
using VRage.FileSystem;
using VRage.Library.Utils;

namespace VRage.Audio
{
    public class MyInMemoryWave : IDisposable
    {
        SoundStream m_stream;
        WaveFormat m_waveFormat;
        AudioBuffer m_buffer;
        private MyWaveBank m_owner;
        private string m_path;

        private int m_references = 1;

        public WaveFormat WaveFormat { get { return m_waveFormat; } }
        public SoundStream Stream { get { return m_stream; } }
        public AudioBuffer Buffer { get { return m_buffer; } }

        public bool Streamed { get; private set; }

        public MyInMemoryWave(MySoundData cue, string path, MyWaveBank owner, bool streamed = false)
        {
            using (var stream = MyFileSystem.OpenRead(path))
            {
                m_owner = owner;
                m_path = path;
                m_stream = new SoundStream(stream);
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

                Streamed = streamed;
            }
        }

        public void Dereference()
        {
            if (Streamed && --m_references <= 0)
                Dispose();
        }

        public void Dispose()
        {
            if (m_buffer != null && m_buffer.Stream != null)
            {
                m_buffer.Stream.Dispose();
            }

            if (Streamed)
            {
                m_owner.LoadedStreamedWaves[m_path].Dispose();
                m_owner.LoadedStreamedWaves.Remove(m_path);
            }
        }

        public void Reference()
        {
            m_references++;
        }
    }
}
