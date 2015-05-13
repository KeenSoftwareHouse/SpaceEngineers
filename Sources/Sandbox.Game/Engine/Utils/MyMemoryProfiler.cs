using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

using Sandbox.Graphics.GUI;
using VRage.Utils;

namespace Sandbox.Engine.Utils
{
//#if MEMORY_PROFILER
    class MyMemoryProfiler
    {
        static List<MyMemoryLogs.MyMemoryEvent> m_managed;
        static List<MyMemoryLogs.MyMemoryEvent> m_native;
        static List<MyMemoryLogs.MyMemoryEvent> m_timed;
        static List<MyMemoryLogs.MyMemoryEvent> m_events;

        static bool m_initialized = false;

        static bool Enabled = false;

        static Vector2 GraphOffset = new Vector2(0.1f, 0.5f);
        static Vector2 GraphSize = new Vector2(0.8f, -0.3f);

        static void SaveSnapshot()
        {

        }

        static MyMemoryLogs.MyMemoryEvent GetEventFromCursor(Vector2 screenPosition)
        {
            // Transform to internal space:
            // x = time
            // y = allocated memory
            Vector2 position = screenPosition;// (screenPosition - GraphOffset) * GraphSize;

            for (int i = 0; i < m_events.Count; i++)
            {
                float startTime = (float)(m_events[i].StartTime - m_events[0].StartTime).TotalSeconds;
                float endTime = (float)(m_events[i].EndTime - m_events[0].StartTime).TotalSeconds;

                if (position.X >= startTime && position.X <= endTime)
                {
                    if (position.Y >= 0 && position.Y <= m_events[i].ProcessEndSize)
                        return m_events[i];
                }
            }

            return null;
        }

//         static public void DumpLoadedMemory(float minimumMegabytesToDump)
//         {
//             float minimumInBytes = 0;// minimumMegabytesToDump * 1024 * 1024;
// 
//             m_events = MyMemoryLogs.GetEvents();
//             StringBuilder sb = new StringBuilder(100000);
//             StringBuilder sb2 = new StringBuilder(100);
// 
//             sb.AppendLine("\n\nMemory dump for events larger than " + minimumMegabytesToDump + "MB:\n");
// 
//             sb.AppendLine("Managed /  Process \n");
// 
//             float sumManaged = 0;
//             float sumProcess = 0;
// 
//             for (int i = 0; i < m_events.Count; i++)
//             {
//                 MyMemoryLogs.MyMemoryEvent ev = m_events[i];
// 
//                 if (ev.Indent == 1)
//                 {
//                     sumManaged += ev.ManagedDelta;
//                     sumProcess += ev.ProcessDelta;
//                 }
// 
//                 if (ev.ManagedDelta >= minimumInBytes || ev.ProcessDelta >= minimumInBytes)
//                 {
//                     sb2.Clear();
//                     sb2.Append(String.Format("{0,7:0.0}", ev.ManagedDelta / (1024 * 1024)));
//                     sb2.Append(" / ");
//                     sb2.Append(String.Format("{0,7:0.0}", ev.ProcessDelta / (1024 * 1024)));
//                     sb2.Append("\t");
// 
//                     for (int k = 0; k < ev.Indent; k++ )
//                         sb2.Append("\t");
//                     
//                     sb2.Append(ev.Name);
//                     sb2.Append(" ");
//                     sb.AppendLine(sb2.ToString());
//                 }
//             }
// 
//             sb.AppendLine("\nTotal managed: " + (sumManaged / (1024 * 1024)) + "   Total process: " + (sumProcess / (1024 * 1024)) + "\n\n");
// 
//             MySandboxGame.Log.Write(sb.ToString());
//         }

        static public void Draw()
        {
            //return;
          //  if (!Enabled) 
            //    return;

            if (!m_initialized)
            {
                m_managed = MyMemoryLogs.GetManaged();
                m_native = MyMemoryLogs.GetNative();
                m_timed = MyMemoryLogs.GetTimed();
                m_events = MyMemoryLogs.GetEvents();
                m_initialized = true;
            }

            float totalTime = 0;
            float maxMB = 0;
            float totalMB = 0;

            if (m_events.Count > 0)
            {
                TimeSpan span = m_events[m_events.Count - 1].EndTime - m_events[0].StartTime;
                totalTime = (float)span.TotalSeconds;
            }

            for (int i = 0; i < m_events.Count; i++)
            {

                //totalTime += m_events[i].DeltaTime;
                totalMB += m_events[i].ProcessDelta;
                maxMB = Math.Max(maxMB, m_events[i].ProcessStartSize);
                maxMB = Math.Max(maxMB, m_events[i].ProcessEndSize);
            }


            Vector2 cursorPos = MyGuiSandbox.MouseCursorPosition;
            //cursorPos
            Vector2 timeAndMemory = (cursorPos - GraphOffset) * new Vector2(GraphSize.X, GraphSize.Y);
            timeAndMemory *= new Vector2(totalTime, maxMB);

            MyMemoryLogs.MyMemoryEvent selectedEvent = GetEventFromCursor(timeAndMemory);

            if (selectedEvent != null)
            {
                StringBuilder sb = new StringBuilder(100);
                sb.Append(selectedEvent.Name);
                // TODO: Par
                //MyDebugDraw.DrawText(new Vector2(100, 100), sb, Color.YellowGreen, 1);
            }
            
            //float time = 0;
            float invMaxMB = maxMB > 0 ? 1 / maxMB : 0;
            float invTotalTime = totalTime > 0 ? 1 / totalTime : 0;

            int colorCnt = 0;
            //int eventCnt = 0;
            foreach (MyMemoryLogs.MyMemoryEvent ev in m_events)
            {
                float startTime = (float)(ev.StartTime - m_events[0].StartTime).TotalSeconds;
                float endTime = (float)(ev.EndTime - m_events[0].StartTime).TotalSeconds;

                float x0 = startTime * invTotalTime;
                //time += ev.DeltaTime;
                float x1 = endTime * invTotalTime + 0.1f;

                float deltaTime = (endTime - startTime) * invTotalTime;
                
//                 if (1000 * Math.Abs(x0 - x1) < 1)
//                 {
//                     x1 += 0.1f;
//                 }

                //x1 = ++eventCnt / 600.0f;

                float y0 = ev.ManagedStartSize * invMaxMB;
                float y1 = ev.ManagedEndSize * invMaxMB;

                float z0 = ev.ProcessStartSize * invMaxMB;
                float z1 = ev.ProcessEndSize * invMaxMB;

                Color color = (colorCnt % 2) == 1 ? Color.Green : Color.LightGreen;
                Color color2 = (colorCnt++ % 2) == 1 ? Color.Blue : Color.LightBlue;

                if (ev == selectedEvent)
                {
                    color = Color.Yellow;
                    color2 = Color.Orange;
                }

                // managed memory
/*
                MyDebugDraw.DrawLine2D(new Vector2(x0, y0) * GraphSize + GraphOffset, new Vector2(x1, y1) * GraphSize + GraphOffset, Color.Red, Color.Red);

                MyDebugDraw.DrawTriangle2D(new Vector2(x0, 0) * GraphSize + GraphOffset,
                                            new Vector2(x1, 0) * GraphSize + GraphOffset,
                                            new Vector2(x1, y1) * GraphSize + GraphOffset, color);
                MyDebugDraw.DrawTriangle2D(new Vector2(x1, y1) * GraphSize + GraphOffset,
                                            new Vector2(x0, y0) * GraphSize + GraphOffset,
                                            new Vector2(x0, 0) * GraphSize + GraphOffset, color);

                // process memory
                MyDebugDraw.DrawTriangle2D(new Vector2(x0, y0) * GraphSize + GraphOffset,
                                            new Vector2(x1, y1) * GraphSize + GraphOffset,
                                            new Vector2(x1, z1) * GraphSize + GraphOffset, color2);
                MyDebugDraw.DrawTriangle2D(new Vector2(x1, z1) * GraphSize + GraphOffset,
                                            new Vector2(x0, z0) * GraphSize + GraphOffset,
                                            new Vector2(x0, y0) * GraphSize + GraphOffset, color2);
 */
            }

           
            // Top 20 native
            StringBuilder sb2 = new StringBuilder();
            Vector2 textOffset = new Vector2(100, 500);

            for(int i=0; i<50 && i < m_native.Count; i++)
            {
                //if ( m_native[i].Name.Contains("MyModels"))
                {
                    // native
                    sb2.Clear();
                    sb2.Append(m_native[i].Name);
                    sb2.Append((0.00000095367431f * m_native[i].ManagedDelta).ToString("GC: 0.0 MB "));
                    //sb2.Append((0.00000095367431f * m_native[i].ProcessDelta).ToString("Process: 0.0 MB "));

                    // TODO: Par
                    //float x_offset = MyDebugDraw.DrawText(textOffset, sb2, Color.Red, 0.7f);
                    //textOffset.Y += 13;

                    // managed
                    sb2.Clear();
                    sb2.Append((0.00000095367431f * m_native[i].ProcessDelta).ToString("Process: 0.0 MB "));

                    // TODO: Par
                    //MyDebugDraw.DrawText(textOffset + new Vector2(x_offset, 0), sb2, Color.Yellow, 0.7f);
                    textOffset.Y += 13;
                }
                
            }

            textOffset = new Vector2(1000, 500);
            textOffset.Y += 10;

            // Top 20 timed
            for (int i = 0; i < 50 && i < m_timed.Count; i++)
            {
                sb2.Clear();
                sb2.Append(m_native[i].Name);
                sb2.Append((m_timed[i].DeltaTime).ToString(" 0.000 s"));

                // TODO: Par
                //MyDebugDraw.DrawText(textOffset, sb2, Color.Yellow, 0.7f);
                textOffset.Y += 13;
            }


//             for (int i = 0; i < m_native.Count && i < 30; i++)
//             {
//                 totalTime += m_native[i].DeltaTime;
//                 maxMB = Math.Max(maxMB, m_native[i].ProcessStartSize);
//                 maxMB = Math.Max(maxMB, m_native[i].ProcessEndSize);
// 
// //                 float sizeInMB = m_native[i].ManagedDelta * 1.0f / (1024 * 1024);
// //                 MySandboxGame.Log.WriteLine(m_native[i].Name + sizeInMB.ToString());
//             }
// 
//             for (int i = 0; i < m_native.Count && i < 30; i++)
//             {
// 
//             }


        //    MyDebugDraw.DrawTriangle2D(new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(1, 1), Color.Yellow);

          //  MyDebugDraw.DrawLine2D(new Vector2(0, 0), new Vector2(1000, 1000), Color.Red, Color.GreenYellow);
            

//             m_managed = MyMemoryLogs.
// 
//             m_managed.Sort(m_managedComparer);
// 
//             MySandboxGame.Log.WriteLine("\n\n");
//             MySandboxGame.Log.WriteLine("Managed MemoryUsage: \n");
// 
//             float totalMB = 0;
// 
//             for (int i = 0; i < m_events.Count && i < 30; i++)
//             {
//                 float sizeInMB = m_events[i].ManagedDelta * 1.0f / (1024 * 1024);
//                 totalMB += sizeInMB;
//                 MySandboxGame.Log.WriteLine(m_events[i].Name + sizeInMB.ToString());
//             }
// 
//             MySandboxGame.Log.WriteLine("Total Managed MemoryUsage: " + totalMB + " [MB]");
        }
    }

//#endif
}
