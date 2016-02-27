using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Network;

namespace Sandbox.Game.Replication
{
    public class MyFloatingObjectPhysicsStateGroup : MyEntityPhysicsStateGroup
    {
        public new MyFloatingObject Entity { get { return (MyFloatingObject)base.Entity; } }

        public override StateGroupEnum GroupType { get { return StateGroupEnum.FloatingObjectPhysics; } }

        public MyFloatingObjectPhysicsStateGroup(MyFloatingObject entity, IMyReplicable owner)
            : base(entity, owner)
        {
            m_lowPrecisionOrientation = true;
            m_prioritySettings.AcceleratingPriority /= 2;
            m_prioritySettings.LinearMovingPriority /= 2;
            m_prioritySettings.StoppedPriority /= 2;

            m_prioritySettings.AcceleratingUpdateCount *= 2;
            m_prioritySettings.LinearMovingUpdateCount *= 2;
            m_prioritySettings.StoppedUpdateCount *= 2;
        }

        protected override float GetGroupPriority(int frameCountWithoutSync, VRage.Network.MyClientInfo client, MyEntityPhysicsStateGroup.PrioritySettings settings)
        {
            return base.GetGroupPriority(frameCountWithoutSync, client, settings);
        }
    }
}
