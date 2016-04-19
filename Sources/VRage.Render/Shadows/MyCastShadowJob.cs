#region Using Statements

using System.Collections.Generic;
using VRageMath;
using System;
using ParallelTasks;
using VRageRender.Utils;

#endregion

namespace VRageRender.Shadows
{
    /// <summary>
    /// Sensor element used for sensors
    /// </summary>
    class MyCastShadowJob : IWork
    {   
        MyRenderObject m_renderObject;

        public bool VisibleFromSun;

        public MyCastShadowJob(MyRenderObject entity)
        {
            m_renderObject = entity;
            VisibleFromSun = false;
            //m_entity.OnClose += m_entity_OnMarkForClose;
        }
             /*
        void m_entity_OnMarkForClose(MyEntity obj)
        {
            m_renderObject = null;
        }       */

        List<MyLineSegmentOverlapResult<MyElement>> m_overlapList = new List<MyLineSegmentOverlapResult<MyElement>>();

        public unsafe void DoWork(WorkData workData = null)
        {
            try
            {
                //MyEntities.EntityCloseLock.AcquireShared();

                if (m_renderObject == null)
                    return;

                if (m_renderObject is MyRenderVoxelCell)
                {
                }
                              
                Vector3 directionToSunNormalized = -MyRender.Sun.Direction;

                VisibleFromSun = false;

                var line2 = new LineD(m_renderObject.WorldVolume.Center, m_renderObject.WorldVolume.Center + directionToSunNormalized * MyShadowRenderer.SHADOW_MAX_OFFSET * 0.5f);
                var result2 = MyRender.GetAnyIntersectionWithLine(MyRender.ShadowPrunning, ref line2, m_renderObject, null, m_overlapList);
                VisibleFromSun |= (result2 == null); //if nothing hit, its visible from sun

                if (m_renderObject.FastCastShadowResolve)
                    return;

                Vector3D* corners = stackalloc Vector3D[8];
                m_renderObject.GetCorners(corners);

                for (int i = 0; i < 8; i++)
                {
                    LineD line = new LineD(corners[i], corners[i] + directionToSunNormalized * MyShadowRenderer.SHADOW_MAX_OFFSET * 0.5f);
                    var result = MyRender.GetAnyIntersectionWithLine(MyRender.ShadowPrunning, ref line, m_renderObject, null, m_overlapList);

                    VisibleFromSun |= (result == null);

                    if (VisibleFromSun)
                        break;
                }

                if (!VisibleFromSun)
                {
                }
            }
            finally
            {      /*
                if (m_renderObject != null)
                {
                    m_renderObject.OnClose -= m_entity_OnMarkForClose;
                }
                 */
               // MyEntities.EntityCloseLock.ReleaseShared();
            }
        }

        public WorkOptions Options
        {
            get { return new WorkOptions() { MaximumThreads = 1 }; }
        } 
    }
}
