using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Replication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Replication;
using VRageMath;


namespace Sandbox.Game.Replication
{
    class MyMeteorReplicable : MyEntityReplicableBaseEvent<MyMeteor>
    {
        #region IMyReplicable Implementation

        public MyMeteorReplicable()
        {
            m_baseVisibility = 5000;
        }
 
        public override void OnDestroy()
        {
            if (Instance != null && Instance.Save)
            {
                Instance.Close();
            }
        }
        #endregion
    }
}
