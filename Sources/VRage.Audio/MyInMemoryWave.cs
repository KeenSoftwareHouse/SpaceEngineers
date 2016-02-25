using SharpDX.Multimedia;
using SharpDX.XAudio2;
using System;
using VRage.Data.Audio;
using VRage.FileSystem;
using VRage.Library.Utils;

namespace VRage.Audio
{
    class MyInMemoryWave : IDisposable
    {
        SoundStream m_stream;
        WaveFormat m_waveFormat;
        AudioBuffer m_buffer;

        public WaveFormat WaveFormat { get { return m_waveFormat; } }
        public SoundStream Stream { get { return m_stream; } }
        public AudioBuffer Buffer { get { return m_buffer; } }

        public MyInMemoryWave(MySoundData cue, string path)
        {
            using (var stream = MyFileSystem.OpenRead(path))
            {
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
            }
        }

        public void Dispose()
        {
            m_stream.Dispose();
            m_buffer.Stream.Dispose();
        }
    }
}
