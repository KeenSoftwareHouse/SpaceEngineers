using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.World;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.Entity;

namespace Sandbox.Game.WorldEnvironment.Modules
{
    /// <summary>
    /// The environmental entity cacher will keep entity references for some time and then close them.
    /// 
    /// This is useful when multiple sector lods support entities because the entity would be deleted
    /// and then re-created during the transition.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class MyEnvironmentalEntityCacher : MySessionComponentBase
    {
        #region Caching

        #region Private members and types
        private const long EntityPreserveTime = 1000; // One second should be enough

        private HashSet<long> m_index;

        private struct EntityReference
        {
            public MyEntity Entity;
        }

        private MyBinaryStructHeap<long, EntityReference> m_entities;

        #endregion


        public void QueueEntity(MyEntity entity)
        {
            long time = Time();
            time += EntityPreserveTime;

            m_entities.Insert(new EntityReference
            {
                Entity = entity
            }, time);

            m_index.Add(entity.EntityId);

            if (UpdateOrder == MyUpdateOrder.NoUpdate)
            {
                SetUpdateOrder(MyUpdateOrder.AfterSimulation);
            }
        }

        public MyEntity GetEntity(long entityId)
        {
            if (m_index.Remove(entityId))
            {
                var e = m_entities.Remove(entityId).Entity;
                Debug.Assert(e != null);

                return e;
            }
            return null;
        }

        public override void UpdateAfterSimulation()
        {
            long time = Time();

            while (m_entities.Count > 0 && m_entities.MinKey() < time)
            {
                m_index.Remove(m_entities.RemoveMin().Entity.EntityId);
            }

            if (m_entities.Count == 0)
            {
                SetUpdateOrder(MyUpdateOrder.NoUpdate);
            }
        }

        /// <summary>
        /// Get the current game time in milliseconds.
        /// </summary>
        /// <returns></returns>
        private static long Time()
        {
            return MySession.Static.ElapsedGameTime.Ticks / TimeSpan.TicksPerMillisecond;
        }

        #endregion
    }
}
