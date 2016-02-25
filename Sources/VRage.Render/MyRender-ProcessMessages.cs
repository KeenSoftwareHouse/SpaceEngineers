using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using VRage;
using VRageRender.Profiler;
using VRageRender.RenderObjects;
using VRageRender.Textures;

namespace VRageRender
{
    internal delegate bool MessageFilterDelegate(MyRenderMessageEnum messageType);

    internal static partial class MyRender
    {
        internal static MessageFilterDelegate m_unloadFilter = UnloadMessageFilter;
        
        internal static bool UnloadMessageFilter(MyRenderMessageEnum messageType)
        {
            switch (messageType)
            {
                case MyRenderMessageEnum.UnloadTexture:
                case MyRenderMessageEnum.UnloadModel:
                case MyRenderMessageEnum.RemoveRenderObject:
                case MyRenderMessageEnum.UpdateHDRSettings:
                case MyRenderMessageEnum.UpdateAntiAliasSettings:
                case MyRenderMessageEnum.UpdateVignettingSettings:
                case MyRenderMessageEnum.UpdateColorMappingSettings:
                case MyRenderMessageEnum.UpdateContrastSettings:
                case MyRenderMessageEnum.UpdateChromaticAberrationSettings:
                case MyRenderMessageEnum.UpdateSSAOSettings:
                case MyRenderMessageEnum.UpdateFogSettings:
                case MyRenderMessageEnum.UpdateGodRaysSettings:
                case MyRenderMessageEnum.CloseVideo:
                case MyRenderMessageEnum.HideDecals:
                case MyRenderMessageEnum.UnloadData:
                    return true;

                default:
                    return false;
            }
        }

        static void ProcessMessageQueue(MessageFilterDelegate filter = null)
        {
            MyRenderProxy.AssertRenderThread();

            while (true)
            {
                bool isPreFrame;
                MyUpdateFrame frame = SharedData.GetRenderFrame(out isPreFrame);
                if (frame == null)
                    return;

                MyRender.CurrentUpdateTime = frame.UpdateTimestamp;

                if (isPreFrame)
                {
                    MyRender.GetRenderProfiler().StartProfilingBlock("ProcessMessageQueue.ProcessPreFrame");
                    ProcessPreFrame(frame, filter);
                    MyRender.GetRenderProfiler().EndProfilingBlock();
                    SharedData.ReturnPreFrame(frame);
                }
                else
                {
                    MyRender.GetRenderProfiler().StartProfilingBlock("ProcessMessageQueue.ProcessRenderFrame");
                    ProcessRenderFrame(frame, filter);
                    MyRender.GetRenderProfiler().EndProfilingBlock();

                    MyRender.GetRenderProfiler().StartProfilingBlock("ProcessMessageQueue.MyRenderClipmap.UpdateQueued");
                    MyRenderClipmap.UpdateQueued();
                    MyRender.GetRenderProfiler().EndProfilingBlock();
                    return;
                }
            }
        }

        private static void ProcessRenderFrame(MyUpdateFrame frame, MessageFilterDelegate filter = null)
        {
            for (int i = 0; i < frame.RenderInput.Count; i++)
            {
                var msg = frame.RenderInput[i];

                if (msg == null)
                    continue;

                if ((filter == null || filter(msg.MessageType)))
                {
                    ProcessMessage(msg);

                    if (msg.MessageClass == MyRenderMessageType.StateChangeOnce)
                    {
                        // TODO: Remove from render input, don't just set to null
                        frame.RenderInput[i] = null;
                        MyRenderProxy.MessagePool.Return(msg);
                    }
                }                
            }
            frame.Processed = true;
        }

        private static void ProcessPreFrame(MyUpdateFrame frame, MessageFilterDelegate filter = null)
        {
            if (!frame.Processed)
            {
                foreach (var msg in frame.RenderInput)
                {
                    if (msg == null)
                        continue;

                    if ((filter == null || filter(msg.MessageType)) && msg.MessageClass != MyRenderMessageType.Draw && msg.MessageClass != MyRenderMessageType.DebugDraw)
                    {
                        ProcessMessage(msg);
                    }
                }

                Debug.Assert(m_drawMessages.Count == 0 && m_debugDrawMessages.Count == 0, "Draw messages was skipped, but there's draw messages!");
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

        static void ClearDrawMessages()
        {
            MyRenderProxy.AssertRenderThread();
            
            m_drawMessages.Clear();
            m_debugDrawMessages.Clear();
        }

        internal static void EnqueueDrawMessage(MyRenderMessageBase drawMessage)
        {
            Debug.Assert(drawMessage.MessageClass == MyRenderMessageType.Draw, "Enqueuing non-draw message");
            m_drawMessages.Enqueue(drawMessage);
        }

        internal static void EnqueueDebugDrawMessage(MyRenderMessageBase drawMessage)
        {
            Debug.Assert(drawMessage.MessageClass == MyRenderMessageType.DebugDraw, "Enqueuing non-draw message");
            m_debugDrawMessages.Enqueue(drawMessage);
        }

    }
}
