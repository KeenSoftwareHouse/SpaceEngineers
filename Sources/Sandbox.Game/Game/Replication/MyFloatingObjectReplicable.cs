using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Network;

namespace Sandbox.Game.Replication
{
    /// <summary>
    /// This class creates replicable object for MyReplicableEntity : MyEntity
    /// </summary>    
    public class MyFloatingObjectReplicable : MyEntityReplicableBaseEvent<MyFloatingObject>
    {
        private StateGroups.MyPropertySyncStateGroup m_propertySync;

        protected override IMyStateGroup CreatePhysicsGroup()
        {
            return new StateGroups.MyFloatingObjectPhysicsStateGroup(Instance, this); // Physics synchronized by MyFloatingObjects
        }

        protected override void OnHook()
        {
            base.OnHook();
            m_propertySync = new StateGroups.MyPropertySyncStateGroup(this, Instance.SyncType);
        }

        public override bool OnSave(VRage.Library.Collections.BitStream stream)
        {
            // TODO: Write custom implementation to save bandwidth (not sending object builder)
            // MyFloatingObjectsSpawn.Spawn(MyPhysicalInventoryItem item, MatrixD worldMatrix, MyPhysicsComponentBase motionInheritedFrom = null)
            base.OnSave(stream);

            return true;
        }

        protected override void OnLoad(VRage.Library.Collections.BitStream stream, Action<MyFloatingObject> loadingDoneHandler)
        {
            // TODO: Write custom implementation to save bandwidth (not sending object builder)
            base.OnLoad(stream, loadingDoneHandler);
        }

        public override void GetStateGroups(List<IMyStateGroup> resultList)
        {
            base.GetStateGroups(resultList);
            if (m_propertySync != null && m_propertySync.PropertyCount > 0)
                resultList.Add(m_propertySync);
        }
    }
}
