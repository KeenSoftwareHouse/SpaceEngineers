﻿using System.Diagnostics;
using System.Windows.Forms;

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
                    GetRenderProfiler().StartProfilingBlock("ProcessPreFrame");

                    ProcessPreFrame(frame);

                    GetRenderProfiler().EndProfilingBlock();

                    SharedData.ReturnPreFrame(frame);
                }
                else
                {
                    GetRenderProfiler().StartProfilingBlock("ProcessRenderFrame");

                    ProcessRenderFrame(frame);

                    GetRenderProfiler().EndProfilingBlock();

                    GetRenderProfiler().StartProfilingBlock("MyClipmapHandler.UpdateQueued");

                    MyClipmapHandler.UpdateQueued();

                    GetRenderProfiler().EndProfilingBlock();

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
                        //Debug.WriteLine("DoEvents()");
                        Application.DoEvents();
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

                    if ((filter == null || filter(msg.MessageType)) && msg.MessageClass != MyRenderMessageType.Draw)
                    {
                        processCtr++;
                        ProcessMessage(msg);
                    }

                    if ((processCtr % 1000) == 0)
                    {
                        m_processStopwatch.Stop();
                        if (m_processStopwatch.Elapsed.TotalSeconds > 0.5f)
                        {
                            //Debug.WriteLine("DoEvents()");
                            Application.DoEvents();
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
