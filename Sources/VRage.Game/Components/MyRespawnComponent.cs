using VRage.Game.ObjectBuilders.ComponentSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;

namespace VRage.Game.Components
{
    [MyComponentBuilder(typeof(MyObjectBuilder_RespawnComponent))]
    public class MyRespawnComponent : MyEntityComponentBase
    {
        private static List<MyRespawnComponent> m_respawns = new List<MyRespawnComponent>(16);

        private MyUseObjectsComponentBase m_useObjectComp;

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

            Container.TryGet<MyUseObjectsComponentBase>(out m_useObjectComp);

            Container.ComponentAdded += Container_ComponentAdded;
            Container.ComponentRemoved += Container_ComponentRemoved;
        }

        public override void OnBeforeRemovedFromContainer()
        {
            m_useObjectComp = null;

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

        public override bool IsSerialized()
        {
            return true;
        }

        void Container_ComponentAdded(Type compType, MyEntityComponentBase component)
        {
            if (compType == typeof(MyUseObjectsComponentBase))
                m_useObjectComp = component as MyUseObjectsComponentBase;
        }

        void Container_ComponentRemoved(Type compType, MyEntityComponentBase component)
        {
            if (compType == typeof(MyUseObjectsComponentBase))
                m_useObjectComp = null;
        }

        public MatrixD GetSpawnPosition(MatrixD worldMatrix)
        {
            ListReader<Matrix> detectorReader = m_useObjectComp.GetDetectors("respawn");

            Debug.Assert(detectorReader.Count > 0, "No spawn positions in respawn!");
            if (detectorReader.Count == 0) return worldMatrix;

            return MatrixD.Multiply(detectorReader.ItemAt(MyUtils.GetRandomInt(detectorReader.Count)), worldMatrix);
        }

        public override string ComponentTypeDebugString
        {
            get { return "Respawn"; }
        }
    }
}
