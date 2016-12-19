using System.Collections.Generic;
using VRage.Render11.Common;
using VRage.Render11.GeometryStage2.Common;
using VRage.Render11.GeometryStage2.Model;
using VRage.Render11.GeometryStage2.Rendering;
using VRageRender;

namespace VRage.Render11.GeometryStage2.Instancing
{
    class MyInstanceManager : IManager, IManagerUnloadData
    {
        HashSet<MyInstanceComponent> m_instances = new HashSet<MyInstanceComponent>();

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

        public void UpdateLods(List<MyRenderPass> renderPasses, List<MyInstanceComponent>[] visibleInstances)
        {
            {
                m_tmpActivePassIds.Clear();
                foreach (var pass in renderPasses)
                    m_tmpActivePassIds.Add(pass.PassId);
            }
            MyLodStrategyPreprocessor preprocessor = MyLodStrategyPreprocessor.Perform();
            {
                foreach (var pass in renderPasses)
                    foreach (var instance in visibleInstances[pass.PassId])
                        instance.UpdateLod(m_tmpActivePassIds, preprocessor);
            }
        }

        void IManagerUnloadData.OnUnloadData()
        {
            MyRenderProxy.Assert(m_instances.Count == 0, "Some of the actors/instances have not been removed correctly");
        }
    }
}
