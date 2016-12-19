using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Instrumentation;
using SharpDX.Mathematics.Interop;
using Valve.VR;
using VRage;
using VRage.Library.Utils;
using VRage.Profiler;
using VRage.Render11.Common;
using VRage.Render11.Profiler;
using VRage.Render11.Resources;
using VRageMath;
using Matrix = VRageMath.Matrix;
using Vector3 = VRageMath.Vector3;

namespace VRageRender
{
    class MyEnvironmentProbe : IManager, IManagerUnloadData
    {
        int m_state;

        bool m_isInit;
        MyTimeSpan m_lastUpdateTime;
        Vector3D m_position;

        internal IUavArrayTexture Cubemap;
        internal const int CubeMapResolution = MyRenderSettings.EnvMapResolution;
        IUavArrayTexture m_workCubemap;
        IUavArrayTexture m_workCubemapPrefiltered;
        IUavArrayTexture m_prevWorkCubemapPrefiltered;
        IDepthArrayTexture CubemapDepth;

        const float MAX_BLEND_TIME_S = 5f;

        void AddAllProbes(MyCullQuery cullQuery)
        {
            m_position = MyRender11.Environment.Matrices.CameraPosition;

            for (int i = 0; i < 6; i++)
                AddProbe(i, cullQuery);
        }

        void AddProbe(int nProbe, MyCullQuery cullQuery)
        {
            MyImmediateRC.RC.ClearDsv(CubemapDepth.SubresourceDsv(nProbe), DepthStencilClearFlags.Depth, 0, 0);
            MyImmediateRC.RC.ClearRtv(m_workCubemap.SubresourceRtv(nProbe), Color4.Black);

            var localRenderViewProj =
                PrepareLocalRenderMatrix(MyRender11.Environment.Matrices.CameraPosition - m_position, nProbe);
            var localCullViewProj =
                PrepareLocalCullingMatrix(MyRender11.Environment.Matrices.CameraPosition - m_position, nProbe, MyRender11.Settings.EnvMapDepth);
            var cullViewProj = MatrixD.CreateTranslation(-m_position) * localCullViewProj;

            cullQuery.AddForwardPass(nProbe, ref localRenderViewProj, ref cullViewProj,
                new MyViewport(0, 0, CubeMapResolution, CubeMapResolution), CubemapDepth.SubresourceDsv(nProbe),
                m_workCubemap.SubresourceRtv(nProbe));
            cullQuery.FrustumCullQueries[cullQuery.Size - 1].Type = MyFrustumEnum.EnvironmentProbe;
            cullQuery.FrustumCullQueries[cullQuery.Size - 1].Index = nProbe;
        }

        void PostprocessProbe(int nProbe)
        {
            var viewMatrix = CubeFaceViewMatrix(Vector3.Zero, nProbe);
            var projMatrix = GetProjectionMatrixInfinite();
            MyEnvProbeProcessing.RunForwardPostprocess(m_workCubemap.SubresourceRtv(nProbe),
                CubemapDepth.SubresourceDsv(nProbe), CubemapDepth.SubresourceSrv(nProbe), 
                ref viewMatrix, ref projMatrix);
        }

        void BlendAllProbes()
        {
            float blendWeight =
                (float) Math.Min((MyRender11.CurrentDrawTime - m_lastUpdateTime).Seconds/MAX_BLEND_TIME_S, 1);

            if (blendWeight < 1)
            {
                MyEnvProbeProcessing.Blend(Cubemap, m_prevWorkCubemapPrefiltered, m_workCubemapPrefiltered, blendWeight);
            }
            else if (blendWeight == 1)
            {
                MyImmediateRC.RC.CopyResource(m_workCubemapPrefiltered, m_prevWorkCubemapPrefiltered);
                MyImmediateRC.RC.CopyResource(m_workCubemapPrefiltered, Cubemap);
            }
        }

        internal void UpdateCullQuery(MyCullQuery cullQuery)
        {
            if (MyRender11.IsIntelBrokenCubemapsWorkaround)
                return;

            if (m_isInit == false)
            {
                m_isInit = true;

                // compute mipmapLevels
                int mipmapLevels = 0;
                for (int tmp = MyEnvironmentProbe.CubeMapResolution; tmp != 1;)
                {
                    mipmapLevels++;
                    tmp = tmp/2 + tmp%2;
                }

                MyArrayTextureManager texManager = MyManagers.ArrayTextures;
                Cubemap = texManager.CreateUavCube("MyEnvironmentProbe.CubemapPrefiltered",
                    MyEnvironmentProbe.CubeMapResolution, Format.R16G16B16A16_Float, mipmapLevels);
                m_workCubemap = texManager.CreateUavCube("MyEnvironmentProbe.WorkCubemap",
                    MyEnvironmentProbe.CubeMapResolution, Format.R16G16B16A16_Float, mipmapLevels);
                m_workCubemapPrefiltered = texManager.CreateUavCube(
                    "MyEnvironmentProbe.WorkCubemapPrefiltered", MyEnvironmentProbe.CubeMapResolution,
                    Format.R16G16B16A16_Float, mipmapLevels);
                m_prevWorkCubemapPrefiltered =
                    texManager.CreateUavCube("MyEnvironmentProbe.PrevWorkCubemapPrefiltered",
                        MyEnvironmentProbe.CubeMapResolution, Format.R16G16B16A16_Float, mipmapLevels);
                CubemapDepth = MyManagers.ArrayTextures.CreateDepthCube("MyEnvironmentProbe.CubemapDepth",
                    MyEnvironmentProbe.CubeMapResolution, Format.R24G8_Typeless, Format.R24_UNorm_X8_Typeless,
                    Format.D24_UNorm_S8_UInt);

                m_lastUpdateTime = MyTimeSpan.Zero;
                m_state = 0;

                AddAllProbes(cullQuery);
            }
            else
            {
                if (m_lastUpdateTime == MyTimeSpan.Zero)
                    AddAllProbes(cullQuery);
                else
                {
                    if (m_state == 0)
                        m_position = MyRender11.Environment.Matrices.CameraPosition;
                    if (m_state < 6)
                        AddProbe(m_state, cullQuery);
                }
            }
        }

        internal void FinalizeEnvProbes()
        {
            if (MyRender11.IsIntelBrokenCubemapsWorkaround)
                return;

            ProfilerShort.Begin("FinalizeEnvProbes");
            MyGpuProfiler.IC_BeginBlock("FinalizeEnvProbes");
            if (m_lastUpdateTime == MyTimeSpan.Zero)
            {
                for (int i = 0; i < 6; i++)
                    PostprocessProbe(i);

                MyGpuProfiler.IC_BeginBlock("BuildMipmaps");
                MyEnvProbeProcessing.BuildMipmaps(m_workCubemap);
                MyGpuProfiler.IC_EndBlock();

                MyGpuProfiler.IC_BeginBlock("Prefilter");
                MyEnvProbeProcessing.Prefilter(m_workCubemap, m_workCubemapPrefiltered);
                MyGpuProfiler.IC_EndBlock();

                MyGpuProfiler.IC_BeginBlock("CopyResource");
                MyImmediateRC.RC.CopyResource(m_workCubemapPrefiltered, m_prevWorkCubemapPrefiltered);
                MyImmediateRC.RC.CopyResource(m_workCubemapPrefiltered, Cubemap);
                MyGpuProfiler.IC_EndBlock();

                m_lastUpdateTime = MyRender11.CurrentDrawTime;
            }
            else
            {
                if (m_state >= 6 && m_state < 12)
                    PostprocessProbe(m_state - 6);
                else if (m_state >= 12)
                {
                    MyGpuProfiler.IC_BeginBlock("BlendAllProbes");
                    BlendAllProbes();
                    MyGpuProfiler.IC_EndBlock();
                }

                if (m_state == 12)
                {
                    MyGpuProfiler.IC_BeginBlock("BuildMipmaps");
                    m_lastUpdateTime = MyRender11.CurrentDrawTime;
                    // whole cubemap is rendered and postprocessed, we can use it
                    MyEnvProbeProcessing.BuildMipmaps(m_workCubemap);
                    MyGpuProfiler.IC_EndBlock();

                    MyGpuProfiler.IC_BeginBlock("Prefilter");
                    MyEnvProbeProcessing.Prefilter(m_workCubemap, m_workCubemapPrefiltered);
                    MyGpuProfiler.IC_EndBlock();
                }

                m_state++;
                MyTimeSpan timeForNextCubemap = m_lastUpdateTime +
                                                MyTimeSpan.FromSeconds(MyEnvironmentProbe.MAX_BLEND_TIME_S);
                if (m_state > 12 && MyRender11.CurrentDrawTime > timeForNextCubemap)
                {
                    m_state = 0; // Time is up, we need to render another environment map
                }

            }
            MyGpuProfiler.IC_EndBlock();
            ProfilerShort.End();
        }

        static Matrix CubeFaceViewMatrix(Vector3 pos, int faceId)
        {
            Matrix viewMatrix = Matrix.Identity;
            switch (faceId)
            {
                case 0:
                    viewMatrix = Matrix.CreateLookAt(pos, pos + Vector3.Left, Vector3.Up);
                    break;
                case 1:
                    viewMatrix = Matrix.CreateLookAt(pos, pos + Vector3.Right, Vector3.Up);
                    break;
                case 2:
                    viewMatrix = Matrix.CreateLookAt(pos, pos + Vector3.Up, -Vector3.Backward);
                    break;
                case 3:
                    viewMatrix = Matrix.CreateLookAt(pos, pos + Vector3.Down, -Vector3.Forward);
                    break;
                case 4:
                    viewMatrix = Matrix.CreateLookAt(pos, pos + Vector3.Backward, Vector3.Up);
                    break;
                case 5:
                    viewMatrix = Matrix.CreateLookAt(pos, pos + Vector3.Forward, Vector3.Up);
                    break;
            }

            return viewMatrix;
        }

        static Matrix GetProjectionMatrix(float farPlane)
        {
            return Matrix.CreatePerspectiveFovRhComplementary((float)Math.PI * 0.5f, 1, 0.01f, farPlane);
        }
        static Matrix GetProjectionMatrixInfinite()
        {
            //return Matrix.CreatePerspectiveFieldOfView((float)Math.PI * 0.5f, 1, 0.01f, farPlane);
            return Matrix.CreatePerspectiveFovRhInfiniteComplementary((float)Math.PI * 0.5f, 1, 0.01f);
        }
        static Matrix PrepareLocalRenderMatrix(Vector3 pos, int faceId)
        {
            var projection = GetProjectionMatrixInfinite();
            Matrix viewMatrix = CubeFaceViewMatrix(pos, faceId);
            return viewMatrix * projection;
        }

        static Matrix PrepareLocalCullingMatrix(Vector3 pos, int faceId, float farPlane)
        {
            var projection = GetProjectionMatrix(farPlane);
            Matrix viewMatrix = CubeFaceViewMatrix(pos, faceId);
            return viewMatrix * projection;
        }

        void IManagerUnloadData.OnUnloadData()
        {
            m_lastUpdateTime = MyTimeSpan.Zero;
        }
    }
}
