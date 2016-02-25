using System;
using System.Collections;
using VRageMath;

namespace VRageRender
{
    class MyOcclusionCuller : MyVisibilityCuller
    {
        //private m_distanceQueue;
        //private m_queryQueue;
        //private m_invisibleQueue;
        //private m_visibleQueue;

        private const int batchSize = 30;

        protected override void DispatchCullQuery(MyCullQuery frustumCullQueries, MyDynamicAABBTreeD renderables)
        {
            throw new NotImplementedException();
        }

        protected override void ProcessCullQueryResults(MyCullQuery cullQuery)
        {
            throw new NotImplementedException();
        }
    }
}
