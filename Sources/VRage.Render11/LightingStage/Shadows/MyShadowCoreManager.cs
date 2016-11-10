using System.Collections.Generic;
using VRage.Generics;
using VRage.Render11.Common;
using VRage.Render11.LightingStage.Shadows.Internal;
using VRage.Render11.Resources;
using VRageMath;
using VRageRender;

namespace VRage.Render11.LightingStage.Shadows
{
    struct MyProjectionInfo
    {
        internal MatrixD WorldToProjection;
        internal MatrixD LocalToProjection;
        internal Vector3D WorldCameraOffsetPosition;

        internal MatrixD CurrentLocalToProjection { get { return MatrixD.CreateTranslation(MyRender11.Environment.Matrices.CameraPosition - WorldCameraOffsetPosition) * LocalToProjection; } }
    }

    struct MyShadowmapQuery
    {
        internal IDsvBindable DepthBuffer;
        internal MyViewport Viewport;
        internal MyProjectionInfo ProjectionInfo;
        internal Vector3 ProjectionDir;
        internal float ProjectionFactor;
        internal MyFrustumEnum QueryType;
        internal int Index;

        internal HashSet<uint> IgnoredEntities;
    }

    class MyShadowCoreManager : IManager, IManagerDevice
    {
        internal MyShadowsSettings Settings = new MyShadowsSettings();

        MyObjectsPool<MySingleShadowmap> m_objectsPoolSingleShadowmap = new MyObjectsPool<MySingleShadowmap>(4);
        MyObjectsPool<MyCascadeShadowMap> m_objectsPoolCsm = new MyObjectsPool<MyCascadeShadowMap>(1);

        MyPostprocessMarkCascades m_markCascades = new MyPostprocessMarkCascades();
        MyPostprocessShadows m_postprocessHardShadows = new MyPostprocessShadows(MyPostprocessShadows.Type.HARD);
        MyPostprocessShadows m_postprocessSimpleShadows = new MyPostprocessShadows(MyPostprocessShadows.Type.SIMPLE);
        MyPostprocessRedToAll m_postprocessRedToAll = new MyPostprocessRedToAll();

        List<IManagerDevice> m_registeredManagerDevices = new List<IManagerDevice>();

        public MyShadowCoreManager()
        {
            m_registeredManagerDevices.Add(m_markCascades);
            m_registeredManagerDevices.Add(m_postprocessHardShadows);
            m_registeredManagerDevices.Add(m_postprocessSimpleShadows);
            m_registeredManagerDevices.Add(m_postprocessRedToAll);
        }

        void IManagerDevice.OnDeviceInit()
        {
            foreach (var manager in m_registeredManagerDevices)
                manager.OnDeviceInit();
        }

        void IManagerDevice.OnDeviceReset()
        {
            foreach (var manager in m_registeredManagerDevices)
                manager.OnDeviceReset();
        }

        void IManagerDevice.OnDeviceEnd()
        {
            foreach (var manager in m_registeredManagerDevices)
                manager.OnDeviceEnd();
        }

        public ICascadeShadowMap CreateCsm(int texSize, int numSlices)
        {
            MyCascadeShadowMap csm;
            m_objectsPoolCsm.AllocateOrCreate(out csm);
            csm.Init(texSize, numSlices);
            return csm;
        }

        public void DisposeCsm(ICascadeShadowMap csm)
        {
            MyCascadeShadowMap myCsm = (MyCascadeShadowMap)csm;
            MyRenderProxy.Assert(!m_objectsPoolCsm.Active.Contains(myCsm), "Shadowmap is not active, maybe it is disposed already.");
            myCsm.Destroy();
            m_objectsPoolCsm.Deallocate(myCsm);
        }

        public ISingleShadowmap CreateSingleShadowmap(int texSize)
        {
            MySingleShadowmap shadowmap;
            m_objectsPoolSingleShadowmap.AllocateOrCreate(out shadowmap);
            shadowmap.Init(texSize);
            return shadowmap;
        }

        public void DisposeSingleShadowmap(ISingleShadowmap shadowmap)
        {
            MySingleShadowmap myShadowmap = (MySingleShadowmap) shadowmap;
            MyRenderProxy.Assert(!m_objectsPoolSingleShadowmap.Active.Contains(myShadowmap), "Shadowmap is not active, maybe it is disposed already.");
            myShadowmap.Destroy();
            m_objectsPoolSingleShadowmap.Deallocate(myShadowmap);
        }

        public void DisposeAll()
        {
            foreach (var shadowmap in m_objectsPoolSingleShadowmap.Active)
            {
                shadowmap.Destroy();
                m_objectsPoolSingleShadowmap.MarkForDeallocate(shadowmap);
            }
            m_objectsPoolSingleShadowmap.DeallocateAllMarked();

            foreach (var shadowmap in m_objectsPoolCsm.Active)
            {
                shadowmap.Destroy();
                m_objectsPoolCsm.MarkForDeallocate(shadowmap);
            }
            m_objectsPoolCsm.DeallocateAllMarked();
        }

        public void PrepareShadowmapQueries(ref List<MyShadowmapQuery> shadowmapQueries)
        {
            if (Settings.NewData.FreezeShadowMaps)
                return;

            int index = 0;
            foreach (var shadowmap in m_objectsPoolSingleShadowmap.Active)
            {
                if (shadowmap.IsUpdated)
                {
                    shadowmap.AddShadowmapQuery(index, shadowmapQueries);
                    index++;
                }
            }

            foreach (var csm in m_objectsPoolCsm.Active)
            {
                if (!Settings.NewData.FreezeShadowVolumePositions)
                    csm.Update(ref Settings);
                csm.AddShadowmapQuery(ref shadowmapQueries);
            }
        }

        public void DrawVolumes()
        {
            foreach (var shadowmap in m_objectsPoolSingleShadowmap.Active)
                shadowmap.DrawVolume();

            foreach (var csm in m_objectsPoolCsm.Active)
                csm.DrawVolumes();
        }

        public void MarkAllCascades(IDepthStencil depthStencil, Matrix worldToProjection, ICascadeShadowMap csm)
        {
            m_markCascades.MarkAllCascades(depthStencil, worldToProjection, csm);
        }

        public void DrawCoverage(IRtvTexture outTex, IDepthStencil depthStencil)
        {
            m_markCascades.DrawCoverage(outTex, depthStencil);
        }

        public void ApplyPostprocess(MyPostprocessShadows.Type type, IRtvTexture outTex, IDepthStencil stencil, ICascadeShadowMap csm,
            ref MyShadowsSettings settings)
        {
            MyPostprocessShadows postprocess = null;
            switch (type)
            {
                case MyPostprocessShadows.Type.HARD:
                    postprocess = m_postprocessHardShadows;
                    break;
                case MyPostprocessShadows.Type.SIMPLE:
                    postprocess = m_postprocessSimpleShadows;
                    break;
                default:
                    MyRenderProxy.Assert(false);
                    break;
            }
            postprocess.Draw(outTex, stencil, csm, ref settings);
        }

        public void CopyRedToAll(IRtvBindable output, ISrvTexture source)
        {
            m_postprocessRedToAll.CopyRedToAll(output, source);
        }
    }
}
