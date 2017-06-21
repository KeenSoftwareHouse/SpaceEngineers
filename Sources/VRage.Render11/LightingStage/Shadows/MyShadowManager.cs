using SharpDX.DXGI;
using System.Collections.Generic;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRage.Render11.Tools;
using VRageRender;

namespace VRage.Render11.LightingStage.Shadows
{
    class MyShadowManager: IManager
    {
        MyShadowCoreManager ShadowCore
        {
            get { return MyManagers.ShadowCore; }
        }
        MyShadowsSettings m_settings;

        ICascadeShadowMap m_mainCsm;


        public MyShadowManager()
        {
            m_mainCsm = ShadowCore.CreateCsm(2048, 7);
            m_mainCsm.CsmPlacementStrategy = new MyCsmRigidPlacementStrategy();

        }

        public void SetSettings(MyShadowsSettings settings)
        {
            m_settings = settings;
            ShadowCore.Settings = settings;
        }

        public float GetSettingsSmallObjectSkipping(int index)
        {
            return m_settings.Cascades[index].SkippingSmallObjectThreshold;
        }

        public void PrepareShadowmapQueries(ref List<MyShadowmapQuery> shadowmapQueries)
        {
            ShadowCore.PrepareShadowmapQueries(ref shadowmapQueries);
        }

        public IBorrowedUavTexture Evaluate()
        {
            MyRenderProxy.Assert(m_mainCsm.SlicesCount == m_settings.NewData.CascadesCount, "Cascades count incostistency");
            if (m_settings.NewData.DrawVolumes)
                ShadowCore.DrawVolumes();
            ShadowCore.MarkAllCascades(MyGBuffer.Main.DepthStencil, MyRender11.Environment.Matrices.ViewProjectionAt0, m_mainCsm);
            if (m_settings.NewData.DisplayCascadeCoverage)
            {
                IBorrowedRtvTexture target = MyManagers.RwTexturesPool.BorrowRtv("MyShadows.Evaluate.CascadeCoverage", Format.R8G8B8A8_SNorm);
                ShadowCore.DrawCoverage(target, MyGBuffer.Main.DepthStencil);
                MyDebugTextureDisplay.Select(target);
                target.Release();
            }

            if (m_settings.NewData.DisplayHardShadows)
            {
                IBorrowedUavTexture hardShadowed =
                    MyManagers.RwTexturesPool.BorrowUav("MyShadows.Evaluate.HardShadowed", Format.R8_UNorm);
                ShadowCore.ApplyPostprocess(MyPostprocessShadows.Type.HARD, hardShadowed, MyGBuffer.Main.DepthStencil, m_mainCsm, ref m_settings);
                MyDebugTextureDisplay.Select(hardShadowed);
                hardShadowed.Release();
            }

            if (m_settings.NewData.DisplaySimpleShadows)
            {
                IBorrowedUavTexture simpleShadowed =
                    MyManagers.RwTexturesPool.BorrowUav("MyShadows.Evaluate.SimpleShadowed", Format.R8_UNorm);
                ShadowCore.ApplyPostprocess(MyPostprocessShadows.Type.SIMPLE, simpleShadowed, MyGBuffer.Main.DepthStencil, m_mainCsm, ref m_settings);
                MyDebugTextureDisplay.Select(simpleShadowed);
                simpleShadowed.Release();
            }

            IBorrowedUavTexture shadowed =
                MyManagers.RwTexturesPool.BorrowUav("MyShadows.Evaluate.Shadowed", Format.R8_UNorm);
            ShadowCore.ApplyPostprocess(MyPostprocessShadows.Type.SIMPLE, shadowed, MyGBuffer.Main.DepthStencil, m_mainCsm, ref m_settings);

            if (m_settings.NewData.EnableFXAAOnShadows)
            {
                IBorrowedUavTexture inputForFXAA =
                    MyManagers.RwTexturesPool.BorrowUav("MyShadows.Evaluate.InputForFXAA", Format.R8G8B8A8_UNorm);
                ShadowCore.CopyRedToAll(inputForFXAA, shadowed);
                IBorrowedUavTexture shadowedWithFXAA =
                    MyManagers.RwTexturesPool.BorrowUav("MyShadows.Evaluate.ShadowedWithFXAA", Format.R8G8B8A8_UNorm);
                MyFXAA.Run(shadowedWithFXAA, inputForFXAA);
                shadowed.Release();
                inputForFXAA.Release();
                shadowed = shadowedWithFXAA;
            }
            return shadowed;
        }

        public IDepthArrayTexture GetCsmForGbuffer()
        {
            return m_mainCsm.DepthArrayTexture;
        }
    }
}
