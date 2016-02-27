using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;


namespace Sandbox.Game.Replication
{
    class MyRopeReplicable : MyEntityReplicableBaseEvent<MyRope>
    {
        public override void OnDestroy()
        {
            MyRopeComponent.RemoveRopeData(Instance.EntityId);

            base.OnDestroy();
        }
    }
}
