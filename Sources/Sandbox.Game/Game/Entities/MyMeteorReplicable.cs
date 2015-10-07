using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Replicables;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Replication;
using VRage.Voxels;
using VRageMath;


namespace Sandbox.Game.Entities
{
    class MyMeteorReplicable : MyEntityReplicableBaseEvent<MyMeteor>
    {
        #region IMyReplicable Implementation
 
        public override void OnDestroy()
        {
            if (Instance.Save)
            {
                Instance.Close();
            }
        }
        #endregion
    }
}
