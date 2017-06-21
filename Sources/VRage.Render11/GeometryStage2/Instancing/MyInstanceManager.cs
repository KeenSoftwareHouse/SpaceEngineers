using System.Collections.Generic;
using VRage.Render11.Common;
using VRage.Render11.GeometryStage2.Common;
using VRage.Render11.GeometryStage2.Model;
using VRage.Render11.GeometryStage2.Rendering;
using VRageMath;
using VRageRender;

namespace VRage.Render11.GeometryStage2.Instancing
{
    class MyInstanceManager : IManager, IManagerUnloadData
    {
        HashSet<MyInstanceComponent> m_instances = new HashSet<MyInstanceComponent>();
        MyGlobalLoddingSettings m_globalLoddingSettings = MyGlobalLoddingSettings.Default;
        Vector3D m_prevCameraPositionOnUpdateLods;

        List<int> m_tmpActivePassIds = new List<int>();

        internal void InitAndRegister(MyInstanceComponent instance, MyModels models, bool isVisible, MyVisibilityExtFlags visibilityExt, MyCompatibilityDataForTheOldPipeline compatibilityData)
        {
            MyRenderProxy.Assert(!m_instances.Contains(instance));

            instance.InitInternal(models, isVisible, visibilityExt, compatibilityData);
            m_instances.Add(instance);
        }

        internal void RemoveInternal(MyInstanceComponent instance)
        {
            MyRenderProxy.Assert(m_instances.Contains(instance));

            m_instances.Remove(instance);
        }

        public void SetLoddingSetting(MyGlobalLoddingSettings settings)
        {
            m_globalLoddingSettings = settings;
        }

        public void UpdateLods(List<MyRenderPass> renderPasses, List<MyInstanceComponent>[] visibleInstances)
        {
            {
                m_tmpActivePassIds.Clear();
                foreach (var pass in renderPasses)
                    m_tmpActivePassIds.Add(pass.PassId);
            }
            MyLodStrategyPreprocessor preprocessor = MyLodStrategyPreprocessor.Perform();
            {
                Vector3D deltaCameraPosition = MyRender11.Environment.Matrices.CameraPosition - m_prevCameraPositionOnUpdateLods;
                m_prevCameraPositionOnUpdateLods = MyRender11.Environment.Matrices.CameraPosition;
                bool isCameraSkipped = deltaCameraPosition.Length() > m_globalLoddingSettings.MaxDistanceForSmoothCameraMovement;

                if (m_globalLoddingSettings.EnableLodSelection)
                {
                    foreach (var pass in renderPasses)
                        foreach (var instance in visibleInstances[pass.PassId])
                            instance.UpdateLodExplicit(m_tmpActivePassIds, m_globalLoddingSettings.LodSelection);
                }
                else if (isCameraSkipped)
                {
                    foreach (var pass in renderPasses)
                        foreach (var instance in visibleInstances[pass.PassId])
                            instance.UpdateLodNoTransition(m_tmpActivePassIds, preprocessor);

                }
                else
                    foreach (var pass in renderPasses)
                        foreach (var instance in visibleInstances[pass.PassId])
                            instance.UpdateLodSmoothly(m_tmpActivePassIds, preprocessor);
            }
        }

        void IManagerUnloadData.OnUnloadData()
        {
            MyRenderProxy.Assert(m_instances.Count == 0, "Some of the actors/instances have not been removed correctly");
        }
    }
}
