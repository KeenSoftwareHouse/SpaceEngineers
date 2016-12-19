
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Render11.Common;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRageRender;

namespace VRage.Render11.Tools
{
    class MyStatsUpdater
    {
        public struct MyPasses
        {
            public int GBufferObjects;

            public int ShadowProjectionObjects;

            public int DrawShadows;
            public int DrawBillboards;

            public void Clear()
            {
                this = default(MyPasses);
            }
        }
        public static int[] CSMObjects = new int[8];
        public static int[] CSMDraws = new int[8];
        public static int[] CSMTris = new int[8];

        public struct MyTimestamps
        {
            static Stopwatch m_stopwatch = Stopwatch.StartNew();

            public long PreviousPresent;
            public long Present;
            public long PreDrawSprites_Draw;
            public long PostDrawSprites_Draw;

            public void Update(ref long Variable)
            {
                double us = (double)m_stopwatch.ElapsedTicks / (double)Stopwatch.Frequency * 1000 * 1000;
                Variable = (long)us;
            }

            public void Update(ref long Variable, ref long PrevVariable)
            {
                PrevVariable = Variable;
                Update(ref Variable);
            }
        }

        public static MyPasses Passes;
        public static MyTimestamps Timestamps;

        static void UpdateResources(string page)
        {
            string group = "Resources count";
            MyStatsDisplay.Write(group, "Array textures", MyManagers.ArrayTextures.GetArrayTexturesCount(), page);
            MyStatsDisplay.Write(group, "DepthStencils", MyManagers.DepthStencils.GetDepthStencilsCount(), page);
            MyStatsDisplay.Write(group, "Custom textures", MyManagers.CustomTextures.GetTexturesCount(), page);

            MyStatsDisplay.Write(group, "Blend states", MyManagers.BlendStates.GetResourcesCount(), page);
            MyStatsDisplay.Write(group, "Depth stencil states", MyManagers.DepthStencilStates.GetResourcesCount(), page);
            MyStatsDisplay.Write(group, "Rasterizer states", MyManagers.RasterizerStates.GetResourcesCount(), page);
            MyStatsDisplay.Write(group, "Sampler states", MyManagers.SamplerStates.GetResourcesCount(), page);

            MyStatsDisplay.Write(group, "Deferred RCs", MyDeferredRenderContextManager.MaxDeferredRCsCount, page);

            group = "Hardware Buffers";
            foreach (var bufferStatistic in MyManagers.Buffers.GetReport())
            {
                MyStatsDisplay.Write(group, bufferStatistic.Name + " TotalBuffers", bufferStatistic.TotalBuffers, page);
                MyStatsDisplay.Write(group, bufferStatistic.Name + " TotalBytes", bufferStatistic.TotalBytes, page);
                MyStatsDisplay.Write(group, bufferStatistic.Name + " TotalBuffersPeak", bufferStatistic.TotalBuffersPeak, page);
                MyStatsDisplay.Write(group, bufferStatistic.Name + " TotalBytesPeak", bufferStatistic.TotalBytesPeak, page);
            }

            group = "File Textures";
            MyFileTextureUsageReport report = MyManagers.FileTextures.GetReport();

            MyStatsDisplay.Write(group, "Total", report.TexturesTotal, page);
            MyStatsDisplay.Write(group, "Loaded", report.TexturesLoaded, page);
            MyStatsDisplay.Write(group, "Memory KB", (int)(report.TotalTextureMemory / 1024), page);
            MyStatsDisplay.Write(group, "Total (peak)", report.TexturesTotalPeak, page);
            MyStatsDisplay.Write(group, "Loaded (peak)", report.TexturesLoadedPeak, page);
            MyStatsDisplay.Write(group, "Memory (peak) KB", (int)(report.TotalTextureMemoryPeak / 1024), page);

            group = "Texture Arrays";
            MyTextureStatistics statistics = MyManagers.FileArrayTextures.Statistics;
            MyStatsDisplay.Write(group, "Total", statistics.TexturesTotal, page);
            MyStatsDisplay.Write(group, "Memory KB", (int)(statistics.TotalTextureMemory / 1024), page);
            MyStatsDisplay.Write(group, "Total (peak)", statistics.TexturesTotalPeak, page);
            MyStatsDisplay.Write(group, "Memory (peak) KB", (int)(statistics.TotalTextureMemoryPeak / 1024), page);

            group = "RW Textures";
            statistics = MyManagers.RwTextures.Statistics;
            MyStatsDisplay.Write(group, "Total", statistics.TexturesTotal, page);
            MyStatsDisplay.Write(group, "Memory KB", (int)(statistics.TotalTextureMemory / 1024), page);
            MyStatsDisplay.Write(group, "Total (peak)", statistics.TexturesTotalPeak, page);
            MyStatsDisplay.Write(group, "Memory (peak) KB", (int)(statistics.TotalTextureMemoryPeak / 1024), page);

            group = "Generated Textures";
            statistics = MyManagers.GeneratedTextures.Statistics;
            MyStatsDisplay.Write(group, "Total", statistics.TexturesTotal, page);
            MyStatsDisplay.Write(group, "Memory KB", (int)(statistics.TotalTextureMemory / 1024), page);
            MyStatsDisplay.Write(group, "Total (peak)", statistics.TexturesTotalPeak, page);
            MyStatsDisplay.Write(group, "Memory (peak) KB", (int)(statistics.TotalTextureMemoryPeak / 1024), page);
        }

        static void UpdatePasses(string page)
        {
            string group = "Draw commands";
            MyStatsDisplay.Write(group, "Shadows", Passes.DrawShadows, page);
            MyStatsDisplay.Write(group, "Billboards", Passes.DrawBillboards, page);

            group = "Cull proxies in passes";
            MyStatsDisplay.Write(group, "GBuffer", Passes.GBufferObjects, page);
            for (int i = 0; i < CSMObjects.Length; i++)
                MyStatsDisplay.Write(group, "CSM" + i, CSMObjects[i], page);
            MyStatsDisplay.Write(group, "Shadow projection", Passes.ShadowProjectionObjects, page);

            group = "Draw call count in passes";
            foreach (var statsTuple in MyRender11.PassStats)
            {
                if (statsTuple.Value.Count == 0)
                    continue;

                int idx = 0;

                foreach (var myPassStats in statsTuple.Value.OrderBy(stats => stats.Key))
                {
                    MyStatsDisplay.Write(group, statsTuple.Key + idx, myPassStats.Value.Draws, page);
                    idx++;
                }
            }

            group = "Tris in passes";
            foreach (var statsTuple in MyRender11.PassStats)
            {
                if (statsTuple.Value.Count == 0)
                    continue;

                int idx = 0;

                foreach (var myPassStats in statsTuple.Value.OrderBy(stats => stats.Key))
                {
                    MyStatsDisplay.Write(group, statsTuple.Key + idx, myPassStats.Value.Triangles, page);
                    idx++;
                }
            }

            Passes.Clear();
            for (int i = 0; i < CSMObjects.Length; i++)
                CSMObjects[i] = 0;
            for (int i = 0; i < CSMTris.Length; i++)
                CSMTris[i] = 0;
        }

        static void UpdateRenderContextStats(string page, string group, MyRenderContextStatistics statistics)
        {
            MyStatsDisplay.Write(group, "Draws", statistics.Draws, page);
            MyStatsDisplay.Write(group, "Dispatches", statistics.Dispatches, page);

            MyStatsDisplay.Write(group, "SetInputLayout", statistics.SetInputLayout, page);
            MyStatsDisplay.Write(group, "SetPrimitiveTopologies", statistics.SetPrimitiveTopologies, page);
            MyStatsDisplay.Write(group, "SetIndexBuffers", statistics.SetIndexBuffers, page);
            MyStatsDisplay.Write(group, "SetVertexBuffers", statistics.SetVertexBuffers, page);
            MyStatsDisplay.Write(group, "SetBlendStates", statistics.SetBlendStates, page);
            MyStatsDisplay.Write(group, "SetDepthStencilStates", statistics.SetDepthStencilStates, page);
            MyStatsDisplay.Write(group, "SetRasterizerStates", statistics.SetRasterizerStates, page);
            MyStatsDisplay.Write(group, "SetViewports", statistics.SetViewports, page);
            MyStatsDisplay.Write(group, "SetTargets", statistics.SetTargets, page);
            MyStatsDisplay.Write(group, "ClearStates", statistics.ClearStates, page);

            MyStatsDisplay.Write(group, "SetConstantBuffers", statistics.SetConstantBuffers, page);
            MyStatsDisplay.Write(group, "SetSamplers", statistics.SetSamplers, page);
            MyStatsDisplay.Write(group, "SetSrvs", statistics.SetSrvs, page);
            MyStatsDisplay.Write(group, "SetVertexShaders", statistics.SetVertexShaders, page);
            MyStatsDisplay.Write(group, "SetGeometryShaders", statistics.SetGeometryShaders, page);
            MyStatsDisplay.Write(group, "SetPixelShaders", statistics.SetPixelShaders, page);
            MyStatsDisplay.Write(group, "SetComputeShaders", statistics.SetComputeShaders, page);
            MyStatsDisplay.Write(group, "SetUavs", statistics.SetUavs, page);
        }

        static MyRenderContextStatistics m_tmpRCStatistics = new MyRenderContextStatistics();
        static List<MyRenderContext> m_tmpListRCs = new List<MyRenderContext>();

        static void UpdateStateChanges(string page)
        {
            UpdateRenderContextStats(page, "Immediate RC calls", MyRender11.RC.GetStatistics());
            MyRender11.RC.ClearStatistics();

            m_tmpRCStatistics.Clear();
            MyRenderProxy.Assert(m_tmpListRCs.Count == 0, "Temporary data are persistently stored in list");
            MyDeferredRenderContextManager rcManager = MyManagers.DeferredRCs;
            for (int i = 0; i < MyDeferredRenderContextManager.MaxDeferredRCsCount; i++)
            {
                MyRenderContext rc = rcManager.AcquireRC();
                m_tmpListRCs.Add(rc);
                m_tmpRCStatistics.Gather(rc.GetStatistics());
                rc.ClearStatistics();
            }
            foreach (MyRenderContext rc in m_tmpListRCs)
                rcManager.FreeRC(rc);
            m_tmpListRCs.Clear();
            UpdateRenderContextStats(page, "Deferred RCs calls", m_tmpRCStatistics);
            m_tmpRCStatistics.Clear();
        }

        static void UpdateGPUParams(string page)
        {
            MyStatsDisplay.WritePersistent("GPU parameters", "Total memory (MBs)", (int)MyRenderProxy.GetAvailableTextureMemory() / 1024 / 1024, page);
        }

        static long m_prevTimestampsUpdate;
        static void UpdateTimestamps(string page)
        {
            long updatesPerSecond = 2;

            long currentTimestamp = 0;
            MyStatsUpdater.Timestamps.Update(ref currentTimestamp);
            long timestampForUpdate = m_prevTimestampsUpdate + 1000 * 1000 / updatesPerSecond;
            if (timestampForUpdate < currentTimestamp)
            {
                long frame = Timestamps.Present - Timestamps.PreviousPresent;
                long sprites = Timestamps.PostDrawSprites_Draw - Timestamps.PreDrawSprites_Draw;
                long frameNoSprites = frame - sprites;

                string group = "Timings";
                MyStatsDisplay.WritePersistent(group, "FPS", (int)(1000.0 * 1000.0 / frame), page);
                MyStatsDisplay.WritePersistent(group, "CPU per frame (us)", (int)frame, page);
                MyStatsDisplay.WritePersistent(group, "CPU no sprites (us)", (int)frameNoSprites, page);
                m_prevTimestampsUpdate = currentTimestamp;
            }
        }

        public static void UpdateStats()
        {
            UpdateStateChanges("Common");
            UpdateGPUParams("Common");
            UpdatePasses("Common");
            UpdateResources("Resources");
            UpdateTimestamps("Common");
            MyManagers.RwTexturesPool.UpdateStats();
        }
    }
}
