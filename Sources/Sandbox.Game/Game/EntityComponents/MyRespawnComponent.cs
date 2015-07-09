using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Components;

namespace VRage.Components
{
    public class MyRespawnComponent : MyEntityComponentBase
    {
        private static List<MyRespawnComponent> m_respawns = new List<MyRespawnComponent>(16);

        public static ListReader<MyRespawnComponent> GetAllRespawns()
        {
            return new ListReader<MyRespawnComponent>(m_respawns);
        }

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();

            if (Container.Entity.InScene)
            {
                Debug.Assert(!m_respawns.Contains(this), "Double add of respawn component to the respawns list");
                m_respawns.Add(this);
            }
        }

        public override void OnBeforeRemovedFromContainer()
        {
            if (Container.Entity.InScene)
            {
                Debug.Assert(m_respawns.Contains(this), "Double remove of respawn component from the respawns list");
                m_respawns.Remove(this);
            }

            base.OnBeforeRemovedFromContainer();
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            Debug.Assert(!m_respawns.Contains(this), "Double add of respawn component to the respawns list");
            m_respawns.Add(this);
        }

        public override void OnRemovedFromScene()
        {
            Debug.Assert(m_respawns.Contains(this), "Double remove of respawn component from the respawns list");
            m_respawns.Remove(this);

            base.OnRemovedFromScene();
        }
    }
}
