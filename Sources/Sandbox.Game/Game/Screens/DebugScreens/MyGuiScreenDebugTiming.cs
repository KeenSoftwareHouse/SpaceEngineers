using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using Sandbox.Engine.Utils;
using VRage.Utils;
using VRage;


using Sandbox.Graphics.GUI;
using Sandbox.Common;
using Sandbox.Game.Debugging;
using Sandbox.Game.Multiplayer;
using Sandbox.Graphics;
using Sandbox.Engine.Physics;
using VRage.Win32;
using VRage.Game;
using VRageRender.Utils;
using MyRenderProxy = VRageRender.MyRenderProxy;

namespace Sandbox.Game.Gui
{
    class MyGuiScreenDebugTiming : MyGuiScreenDebugBase
    {
        static StringBuilder m_debugText = new StringBuilder(1000);

        long m_startTime = 0;
        long m_ticks = 0;
        int m_frameCounter = 0;

        double m_updateLag = 0;

        long m_lastSent = 0;
        long m_lastReceived = 0;

        double m_sentLastSec = 0;
        double m_receivedLastSec = 0;

        public MyGuiScreenDebugTiming()
            : base(new Vector2(0.5f, 0.5f), new Vector2(), null, true)
        {
            m_isTopMostScreen = true;
            m_drawEvenWithoutFocus = true;
            CanHaveFocus = false;
            m_canShareInput = false;
        }

        public override void LoadData()
        {
            base.LoadData();
            MyRenderProxy.DrawRenderStats = MyRenderProxy.MyStatsState.MoveNext;
        }

        public override void UnloadData()
        {
            base.UnloadData();
            MyRenderProxy.DrawRenderStats = MyRenderProxy.MyStatsState.NoDraw;
        }

        public override string GetFriendlyName()
        {
            return "DebugTimingScreen";
        }

        public override bool Update(bool hasFocus)
        {
            m_ticks = MyPerformanceCounter.ElapsedTicks;
            m_frameCounter++;

            double secondsFromStart = MyPerformanceCounter.TicksToMs(m_ticks - m_startTime) / 1000;
            if (secondsFromStart > 1)
            {
                double updateLagOverMeasureTime = (secondsFromStart - m_frameCounter * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);
                m_updateLag = updateLagOverMeasureTime / secondsFromStart * 1000;

                m_startTime = m_ticks;
                m_frameCounter = 0;

                if (Sync.Layer != null)
                {
                    m_sentLastSec = (Sync.Layer.TransportLayer.ByteCountSent - m_lastSent) / secondsFromStart;
                    m_receivedLastSec = (Sync.Layer.TransportLayer.ByteCountReceived - m_lastReceived) / secondsFromStart;

                    m_lastReceived = Sync.Layer.TransportLayer.ByteCountReceived;
                    m_lastSent = Sync.Layer.TransportLayer.ByteCountSent;
                }
            }

            Stats.Timing.Write("FPS", MyFpsManager.GetFps(), VRage.Stats.MyStatTypeEnum.CurrentValue, 0, 0);
            Stats.Timing.Increment("UPS", 1000);
            Stats.Timing.Write("Simulation speed", Sandbox.Engine.Physics.MyPhysics.SimulationRatio, VRage.Stats.MyStatTypeEnum.CurrentValue, 100, 2);
            Stats.Timing.Write("Server simulation speed", Sync.ServerSimulationRatio, VRage.Stats.MyStatTypeEnum.CurrentValue, 100, 2);
            Stats.Timing.WriteFormat("Frame time: {0} ms", MyFpsManager.FrameTime, VRage.Stats.MyStatTypeEnum.CurrentValue, 0, 1);
            Stats.Timing.WriteFormat("Frame avg time: {0} ms", MyFpsManager.FrameTimeAvg, VRage.Stats.MyStatTypeEnum.CurrentValue, 0, 1);
            Stats.Timing.WriteFormat("Frame min time: {0} ms", MyFpsManager.FrameTimeMin, VRage.Stats.MyStatTypeEnum.CurrentValue, 0, 1);
            Stats.Timing.WriteFormat("Frame max time: {0} ms", MyFpsManager.FrameTimeMax, VRage.Stats.MyStatTypeEnum.CurrentValue, 0, 1);
            Stats.Timing.Write("Update lag (per s)", (float)m_updateLag, VRage.Stats.MyStatTypeEnum.CurrentValue, 0, 4);
            Stats.Timing.Write("GC Memory", GC.GetTotalMemory(false), VRage.Stats.MyStatTypeEnum.CurrentValue, 0, 0);
#if !XB1
            Stats.Timing.Write("Process memory", WinApi.WorkingSet, VRage.Stats.MyStatTypeEnum.CurrentValue, 0, 0);
#endif // !XB1
            Stats.Timing.Write("Active particle effects", MyParticlesManager.ParticleEffectsForUpdate.Count, VRage.Stats.MyStatTypeEnum.CurrentValue, 0, 0);
            //Stats.Timing.Write("Billboards total", VRageRender.MyPerformanceCounter.PerCameraDraw11Read.BillboardsDrawn, VRage.Stats.MyStatTypeEnum.CurrentValue, 0, 0);

            if (MyPhysics.GetClusterList() != null)
            {
                double i = 0.0;
                double sum = 0.0;
                double max = 0.0;
                foreach (Havok.HkWorld havokWorld in MyPhysics.GetClusterList())
                {
                    i += 1.0;
                    var value = havokWorld.StepDuration.TotalMilliseconds;
                    sum += value;
                    if (value > max)
                        max = value;
                }
                Stats.Timing.WriteFormat("Physics worlds count: {0}", (float)i, VRage.Stats.MyStatTypeEnum.CurrentValue, 0, 0);
                Stats.Timing.WriteFormat("Physics step time (sum): {0} ms", (float)sum, VRage.Stats.MyStatTypeEnum.CurrentValue, 0, 1);
                Stats.Timing.WriteFormat("Physics step time (avg): {0} ms", (float)(sum / i), VRage.Stats.MyStatTypeEnum.CurrentValue, 0, 1);
                Stats.Timing.WriteFormat("Physics step time (max): {0} ms", (float)max, VRage.Stats.MyStatTypeEnum.CurrentValue, 0, 1);
            }

            if (Sync.Layer != null)
            {
                Stats.Timing.Write("Received KB/s", (float)m_receivedLastSec / 1024, VRage.Stats.MyStatTypeEnum.CurrentValue, 0, 2);
                Stats.Timing.Write("Sent KB/s", (float)m_sentLastSec / 1024, VRage.Stats.MyStatTypeEnum.CurrentValue, 0, 2);
            }

            return base.Update(hasFocus);
        }
    }
}
