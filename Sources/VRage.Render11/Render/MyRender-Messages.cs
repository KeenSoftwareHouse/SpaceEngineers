using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SharpDX.Direct3D11;
using System.Windows.Forms;
using VRageRender.Messages;

namespace VRageRender
{
    partial class MyRender11
    {
        delegate bool MessageFilterDelegate(MyRenderMessageEnum messageType);

        internal static Stopwatch m_processStopwatch = new Stopwatch();

        internal static void ProcessMessageQueue()
        {
            //MyRenderProxy.AssertRenderThread();

            bool ok = true;

            while (ok)
            {
                bool isPreFrame;
                MyUpdateFrame frame = SharedData.GetRenderFrame(out isPreFrame);
                if (frame == null)
                    return;

                CurrentUpdateTime = frame.UpdateTimestamp;
                

                if (isPreFrame)
                {
                    MyRender11.GetRenderProfiler().StartProfilingBlock("ProcessPreFrame");

                    ProcessPreFrame(frame);

                    MyRender11.GetRenderProfiler().EndProfilingBlock();

                    SharedData.ReturnPreFrame(frame);
                }
                else
                {
                    MyRender11.GetRenderProfiler().StartProfilingBlock("ProcessRenderFrame");

                    ProcessRenderFrame(frame);

                    MyRender11.GetRenderProfiler().EndProfilingBlock();

                    MyRender11.GetRenderProfiler().StartProfilingBlock("MyClipmapHandler.UpdateQueued");

                    MyClipmapHandler.UpdateQueued();

                    MyRender11.GetRenderProfiler().EndProfilingBlock();

                    ok = false;
                }
            }
        }

        private static void ProcessRenderFrame(MyUpdateFrame frame, MessageFilterDelegate filter = null)
        {
            int processCtr = 0;
            m_processStopwatch.Restart();

            for (int i = 0; i < frame.RenderInput.Count; i++)
            {
                var msg = frame.RenderInput[i];

                if (msg == null)
                    continue;

                if ((filter == null || filter(msg.MessageType)))
                {
                    ProcessMessage(msg);
                    processCtr++;

                    if (msg.MessageClass == MyRenderMessageType.StateChangeOnce)
                    {
                        frame.RenderInput[i] = null;
                        MyRenderProxy.MessagePool.Return(msg);
                    }
                }

                if ((processCtr % 1000) == 0)
                {
                    m_processStopwatch.Stop();
                    if (m_processStopwatch.Elapsed.TotalSeconds > 0.5f)
                    {
#if !XB1
                        //Debug.WriteLine("DoEvents()");
                        if (MyRenderProxy.EnableAppEventsCall)
                        {
                            Application.DoEvents();
                        }
#endif
						m_processStopwatch.Reset();
					}
                    m_processStopwatch.Start();
                }
            }

            frame.RenderInput.RemoveAll(item => item == null);
            frame.Processed = true;
        }

        private static void ProcessPreFrame(MyUpdateFrame frame, MessageFilterDelegate filter = null)
        {
            int processCtr = 0;
            m_processStopwatch.Restart();

            if (!frame.Processed)
            {
                foreach (var msg in frame.RenderInput)
                {
                    if (msg == null)
                        continue;

                    if ((filter == null || filter(msg.MessageType)) && msg.MessageClass != MyRenderMessageType.Draw && msg.MessageClass != MyRenderMessageType.DebugDraw)
                    {
                        processCtr++;
                        ProcessMessage(msg);
                    }

                    if ((processCtr % 1000) == 0)
                    {
                        m_processStopwatch.Stop();
                        if (m_processStopwatch.Elapsed.TotalSeconds > 0.5f)
                        {
#if !XB1
                            //Debug.WriteLine("DoEvents()");
                            if (MyRenderProxy.EnableAppEventsCall)
                            {
                                Application.DoEvents();
                            }
#endif
							m_processStopwatch.Reset();
                        }
                        m_processStopwatch.Start();
                    }
                }
            }

            // Return messages
            foreach (var msg in frame.RenderInput)
            {
                if (msg != null)
                {
                    MyRenderProxy.MessagePool.Return(msg);
                }
            }

            frame.RenderInput.Clear();
        }
    }
}
