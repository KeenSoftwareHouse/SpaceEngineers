using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Entity;
using VRage.Library.Collections;
using VRage.Network;
using VRageMath;

namespace Sandbox.Game.Replication
{
    public class MyFloatingObjectPhysicsStateGroup : MySmallObjectPhysicsStateGroup
    {
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

    }
}
