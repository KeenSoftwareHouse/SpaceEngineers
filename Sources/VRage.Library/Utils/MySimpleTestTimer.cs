using System;
using System.Diagnostics;
using System.IO;

namespace VRage.Library.Utils
{
    internal struct MySimpleTestTimer : IDisposable
    {
        private string m_name;

        private Stopwatch m_watch;

        public MySimpleTestTimer(string name)
        {
            m_name = name;
            m_watch = new Stopwatch();
            m_watch.Start();
        }

        public void Dispose()
        {
#if !XB1
            File.AppendAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "perf.log"),
                String.Format("{0}: {1:N}ms\n", m_name, m_watch.ElapsedMilliseconds));
#else // XB1
            System.Diagnostics.Debug.Assert(false, "XB1 TODO?");
#endif // XB1
        }
    }
}
