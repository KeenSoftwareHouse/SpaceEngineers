using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders.Components;
using VRage.Game.ObjectBuilders.ComponentSystem;

namespace VRage.Game.Entity.EntityComponents
{
    [MyComponentType(typeof(MyEntityOwnershipComponent))]
    [MyComponentBuilder(typeof(MyObjectBuilder_EntityOwnershipComponent))]
    public class MyEntityOwnershipComponent : MyEntityComponentBase
    {
        #region Private
        private long m_ownerId = 0;
        private MyOwnershipShareModeEnum m_shareMode = MyOwnershipShareModeEnum.All;
        #endregion

        #region Events
        public Action<long, long> OwnerChanged;
        public Action<MyOwnershipShareModeEnum> ShareModeChanged;
        #endregion

        #region Properties
        public long OwnerId
        {
            get { return m_ownerId; }
            set
            {
                if (m_ownerId != value && OwnerChanged != null)
                    OwnerChanged(m_ownerId, value);
                m_ownerId = value;
            }
        }

        public MyOwnershipShareModeEnum ShareMode
        {
            get { return m_shareMode; }
            set
            {
                if (m_shareMode != value && ShareModeChanged != null)
                    ShareModeChanged(value);
                m_shareMode = value;
            }
        }
        #endregion

        #region (De)Serialization
        public override bool IsSerialized()
        {
            return true;
        }

        public override MyObjectBuilder_ComponentBase Serialize(bool copy = false)
        {
            var ob = base.Serialize() as MyObjectBuilder_EntityOwnershipComponent;

            ob.OwnerId = m_ownerId;
            ob.ShareMode = m_shareMode;

            return ob;
        }

        public override void Deserialize(MyObjectBuilder_ComponentBase builder)
        {
            base.Deserialize(builder);

            var ob = builder as MyObjectBuilder_EntityOwnershipComponent;

            m_ownerId = ob.OwnerId;
            m_shareMode = ob.ShareMode;
        }
        #endregion

        public override string ComponentTypeDebugString
        {
            get { return GetType().Name; }
        }
    }
}
