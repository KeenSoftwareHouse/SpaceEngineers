using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Replicables;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRage.Network;
using VRage.ObjectBuilders;

namespace Sandbox.Game.Replicables
{
    class MyCharacterReplicable : MyEntityReplicableBase<MyCharacter>
    {
        protected override IMyStateGroup CreatePhysicsGroup()
        {
            return new MyCharacterPhysicsStateGroup(Instance, this);
        }

        public override float GetPriority(MyClientStateBase state)
        {
            var info = Instance.ControllerInfo;

            if (info != null && info.Controller != null && info.Controller.Player != null &&
                info.Controller.Player.Id.SteamId == state.EndpointId.Value)
            {
                return 1.0f;
            }

            return base.GetPriority(state);
        }
    }
}
