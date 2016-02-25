using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Library.Utils;
using VRageMath;
using VRageRender.Resources;
using Matrix = VRageMath.Matrix;
using Vector3 = VRageMath.Vector3;

namespace VRageRender
{
    internal struct MyEnvironmentProbe
    {
        const float ProbePositionOffset = 2;

        internal int state;

        internal RwTexId cubemapPrefiltered;

        internal MyTimeSpan lastUpdateTime;
        internal float blendWeight;

        internal RwTexId workCubemap;
        internal RwTexId workCubemapPrefiltered;

        internal RwTexId prevWorkCubemapPrefiltered;

        internal Vector3D position;
        internal MyTimeSpan blendT0;
        internal const float MaxBlendTimeS = 5;

        internal static MyEnvironmentProbe Instance = MyEnvironmentProbe.Create();
        private static RwTexId m_cubemapDepth = RwTexId.NULL;

        internal const int CubeMapResolution = 256;

        internal static MyEnvironmentProbe Create()
        {
            var envProbe = new MyEnvironmentProbe();

            envProbe.cubemapPrefiltered = RwTexId.NULL;
            envProbe.workCubemap = RwTexId.NULL;
            envProbe.workCubemapPrefiltered = RwTexId.NULL;
            envProbe.prevWorkCubemapPrefiltered = RwTexId.NULL;

            envProbe.lastUpdateTime = MyTimeSpan.Zero;
            envProbe.state = 0;

            return envProbe;
        }

        internal void ImmediateProbe(MyCullQuery cullQuery)
        {
            // reset
            state = 0;

            var prevState = state;
            StepUpdateProbe(cullQuery);
            while (prevState != state)
            {
                prevState = state;
                StepUpdateProbe(cullQuery);
            }
        }

        internal void StepUpdateProbe(MyCullQuery cullQuery)
        {
            if (state == 0)
            {
                position = MyEnvironment.CameraPosition;// +Vector3.UnitY * 4;
            }

            if (state < 6)
            {
                int faceId = state;
                MyImmediateRC.RC.DeviceContext.ClearDepthStencilView(m_cubemapDepth.SubresourceDsv(faceId), DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1, 0);
                MyImmediateRC.RC.DeviceContext.ClearRenderTargetView(workCubemap.SubresourceRtv(faceId), new Color4(0, 0, 0, 0));

                var localViewProj = Matrix.CreateTranslation(MyEnvironment.CameraPosition - position) * PrepareLocalEnvironmentMatrix(Vector3.Zero, new Vector2I(CubeMapResolution, CubeMapResolution), faceId, 10000.0f);
                var viewProj = MatrixD.CreateTranslation(-position) * localViewProj;

                cullQuery.AddForwardPass(ref localViewProj, ref viewProj, new MyViewport(0, 0, CubeMapResolution, CubeMapResolution), m_cubemapDepth.SubresourceDsv(faceId), workCubemap.SubresourceRtv(faceId));

                ++state;
                return;
            }
        }

        internal void ImmediateFiltering()
        {
            Debug.Assert(state == 6);

            var prevState = state;
            StepUpdateFiltering();
            while (prevState != state)
            {
                prevState = state;
                StepUpdateFiltering();
            }

            UpdateBlending();
        }

        internal void StepUpdateFiltering()
        {
            if (6 <= state && state < 12)
            {
                int faceId = state - 6;
                var matrix = CubeFaceViewMatrix(Vector3.Zero, faceId);
                MyEnvProbeProcessing.RunForwardPostprocess(workCubemap.SubresourceRtv(faceId), m_cubemapDepth.SubresourceSrv(faceId), ref matrix, MyAtmosphereRenderer.GetCurrentAtmosphereId());
                MyEnvProbeProcessing.BuildMipmaps(workCubemap);
                MyEnvProbeProcessing.Prefilter(workCubemap, workCubemapPrefiltered);


                ++state;

                if (state == 12)
                {
                    blendT0 = MyRender11.CurrentDrawTime;
                }

                return;
            }
        }

        internal void UpdateBlending()
        {
            if (state == 12 && blendWeight < 1)
            {
                blendWeight = (float)Math.Min((MyRender11.CurrentDrawTime - blendT0).Seconds / MaxBlendTimeS, 1);

                MyEnvProbeProcessing.Blend(cubemapPrefiltered, prevWorkCubemapPrefiltered, workCubemapPrefiltered, blendWeight);
                Instance.lastUpdateTime = MyRender11.CurrentDrawTime;
            }

            if (state == 12 && blendWeight == 1)
            {
                state = 0;
                MyImmediateRC.RC.DeviceContext.CopyResource(workCubemapPrefiltered.Resource, prevWorkCubemapPrefiltered.Resource);
                blendWeight = 0;
            }

            //    Texture2D.ToFile(MyImmediateRC.RC.Context, workCubemap.Resource, ImageFileFormat.Dds, "c:\\environment.dds");
        }

        internal static void UpdateEnvironmentProbes(MyCullQuery cullQuery)
        {
            if (MyRender11.IsIntelBrokenCubemapsWorkaround)
                return;

            if (m_cubemapDepth == RwTexId.NULL)
            {
                m_cubemapDepth = MyRwTextures.CreateShadowmapArray(MyEnvironmentProbe.CubeMapResolution, MyEnvironmentProbe.CubeMapResolution, 6, Format.R24G8_Typeless, Format.D24_UNorm_S8_UInt, Format.R24_UNorm_X8_Typeless);
            }

            if (Instance.cubemapPrefiltered == RwTexId.NULL)
            {
                Instance.cubemapPrefiltered = MyRwTextures.CreateCubemap(MyEnvironmentProbe.CubeMapResolution, Format.R16G16B16A16_Float, "Environment Prefiltered Probe");

                Instance.workCubemap = MyRwTextures.CreateCubemap(MyEnvironmentProbe.CubeMapResolution, Format.R16G16B16A16_Float, "Environment Probe");
                Instance.workCubemapPrefiltered = MyRwTextures.CreateCubemap(MyEnvironmentProbe.CubeMapResolution, Format.R16G16B16A16_Float, "Environment Prefiltered Probe");

                Instance.prevWorkCubemapPrefiltered = MyRwTextures.CreateCubemap(MyEnvironmentProbe.CubeMapResolution, Format.R16G16B16A16_Float, "Environment Prefiltered Probe");

                Instance.ImmediateProbe(cullQuery);
            }
            else
            {
                Instance.StepUpdateProbe(cullQuery);
            }
        }

        internal static void FinalizeEnvProbes()
        {
            if (MyRender11.IsIntelBrokenCubemapsWorkaround)
                return;

            ProfilerShort.Begin("FinalizeEnvProbes");

            if (Instance.lastUpdateTime == MyTimeSpan.Zero)
            {
                Instance.ImmediateFiltering();
                MyImmediateRC.RC.DeviceContext.CopyResource(Instance.workCubemapPrefiltered.Resource, Instance.prevWorkCubemapPrefiltered.Resource);
            }
            else
            {
                Instance.StepUpdateFiltering();
                Instance.UpdateBlending();
            }
            ProfilerShort.End();
        }

        internal static Matrix CubeFaceViewMatrix(Vector3 pos, int faceId)
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

        internal static Matrix PrepareLocalEnvironmentMatrix(Vector3 pos, Vector2I resolution, int faceId, float farPlane)
        {
            var projection = Matrix.CreatePerspectiveFieldOfView((float)Math.PI * 0.5f, 1, 0.1f, farPlane);
            Matrix viewMatrix = CubeFaceViewMatrix(pos, faceId);
            return viewMatrix * projection;
        }
    }
}
