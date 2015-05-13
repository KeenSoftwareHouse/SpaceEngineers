using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Utils
{
    public class MyMemoryLogs
    {
        public class MyMemoryEvent
        {
            public string Name;
            public bool HasStart = false;
            public bool HasEnd = false;
            public float ManagedStartSize = 0;
            public float ManagedEndSize = 0;
            public float ProcessStartSize = 0;
            public float ProcessEndSize = 0;
            public float DeltaTime = 0;
            public int Id = 0;
            public bool Selected = false;
            public DateTime StartTime;
            public DateTime EndTime;

            public float ManagedDelta
            {
                get { return ManagedEndSize - ManagedStartSize; }
            }

            public float ProcessDelta
            {
                get { return ProcessEndSize - ProcessStartSize; }
            }

            List<MyMemoryEvent> m_childs = new List<MyMemoryEvent>();  
        }

        class MyManagedComparer : IComparer<MyMemoryEvent>
        {
            public int Compare(MyMemoryEvent x, MyMemoryEvent y)  {   return -1 * (x.ManagedDelta.CompareTo(y.ManagedDelta)); }
        }

        class MyNativeComparer : IComparer<MyMemoryEvent>
        {
            public int Compare(MyMemoryEvent x, MyMemoryEvent y)  {   return -1 * (x.ProcessDelta.CompareTo(y.ProcessDelta)); }
        }

        class MyTimedeltaComparer : IComparer<MyMemoryEvent>
        {
            public int Compare(MyMemoryEvent x, MyMemoryEvent y)  {   return -1 * (x.DeltaTime.CompareTo(y.DeltaTime));   }
        }

        static MyManagedComparer m_managedComparer = new MyManagedComparer();
        static MyNativeComparer m_nativeComparer = new MyNativeComparer();
        static MyTimedeltaComparer m_timeComparer = new MyTimedeltaComparer();

        static List<MyMemoryEvent> m_events = new List<MyMemoryEvent>();
        static List<string> m_consoleLogSTART = new List<string>();
        static List<string> m_consoleLogEND = new List<string>();

        static Stack<MyMemoryEvent> m_stack = new Stack<MyMemoryEvent>();

        static int IdCounter = 1;

        static public List<MyMemoryEvent> GetManaged()
        {
            List<MyMemoryEvent> managed = new List<MyMemoryEvent>(m_events);
            managed.Sort(m_managedComparer);
            return managed;
        }

        static public List<MyMemoryEvent> GetNative()
        {
            List<MyMemoryEvent> native = new List<MyMemoryEvent>(m_events);
            native.Sort(m_nativeComparer);
            return native;
        }

        static public List<MyMemoryEvent> GetTimed()
        {
            List<MyMemoryEvent> timed = new List<MyMemoryEvent>(m_events);
            timed.Sort(m_timeComparer);
            return timed;
        }

        static public List<MyMemoryEvent> GetEvents()
        {
            return m_events;
        }

        static public void StartEvent()
        {
            MyMemoryEvent ev = new MyMemoryEvent();

            if (m_consoleLogSTART.Count > 0)
            {
                ev.Name = m_consoleLogSTART[m_consoleLogSTART.Count-1];
                ev.Id = IdCounter++;
                ev.StartTime = DateTime.Now;
                m_consoleLogSTART.Clear();
                m_stack.Push(ev);
            }   
        }

        static public void EndEvent(MyMemoryEvent ev)
        {
             if(m_stack.Count > 0)
             {
                 MyMemoryEvent e = m_stack.Peek();
                 ev.Name = e.Name;
                 ev.Id = e.Id;
                 ev.StartTime = e.StartTime;
                 ev.EndTime = DateTime.Now;
                 m_events.Add(ev);
                 m_stack.Pop();
             }
        }

        static public void AddConsoleLine(string line)
        {
            if (line.EndsWith("START"))
            {
                m_consoleLogEND.Clear();
                line = line.Substring(0, line.Length - 5);

                if (m_stack.Count > 0 && m_stack.Peek().HasStart)
                {
                    m_events[m_events.Count].HasStart = true;
                    m_events[m_events.Count].Name = line;
                }
                else
                    m_consoleLogSTART.Add(line);
            }
            else if (line.EndsWith("END"))
            {
                line = line.Substring(0, line.Length - 5);

                m_consoleLogEND.Add(line);
                m_consoleLogSTART.Clear();
            }
        }

        public static void DumpMemoryUsage()
        {
            m_events.Sort(m_managedComparer);

            MyLog.Default.WriteLine("\n\n");
            MyLog.Default.WriteLine("Managed MemoryUsage: \n");

            float totalMB = 0;

            for (int i = 0; i < m_events.Count && i < 30; i++)
            {
                float sizeInMB = m_events[i].ManagedDelta * 1.0f/(1024*1024);
                totalMB += sizeInMB;
                MyLog.Default.WriteLine(m_events[i].Name + sizeInMB.ToString());
            }

            MyLog.Default.WriteLine("Total Managed MemoryUsage: " + totalMB + " [MB]");

            //////////////////////////////////////////////////////////////////////////

            m_events.Sort(m_nativeComparer);

            MyLog.Default.WriteLine("\n\n");
            MyLog.Default.WriteLine("Process MemoryUsage: \n");

            totalMB = 0;

            for (int i = 0; i < m_events.Count && i < 30; i++)
            {
                float sizeInMB = m_events[i].ProcessDelta * 1.0f / (1024 * 1024);
                totalMB += sizeInMB;
                MyLog.Default.WriteLine(m_events[i].Name + sizeInMB.ToString());
            }

            MyLog.Default.WriteLine("Total Process MemoryUsage: " + totalMB + " [MB]");

            //////////////////////////////////////////////////////////////////////////

            m_events.Sort(m_timeComparer);

            MyLog.Default.WriteLine("\n\n");
            MyLog.Default.WriteLine("Load time comparison: \n");

            float totalTime = 0;

            for (int i = 0; i < m_events.Count && i < 30; i++)
            {
                float deltaTime = m_events[i].DeltaTime;
                totalTime += deltaTime;
                MyLog.Default.WriteLine(m_events[i].Name + " " + deltaTime.ToString());
            }

            MyLog.Default.WriteLine("Total load time: " + totalTime + " [s]");
        }
    }
}
