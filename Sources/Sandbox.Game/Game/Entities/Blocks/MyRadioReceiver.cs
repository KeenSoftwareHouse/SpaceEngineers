#region Using

using Sandbox.Common;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using System.Collections.Generic;
using VRageMath;

#endregion

namespace Sandbox.Game.Entities.Cube
{
    public class MyRadioReceiver : MyDataReceiver
    {
        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            Enabled = true;
        }

        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();
            Enabled = false;
        }

        protected override void GetAllBroadcastersInMyRange(ref HashSet<MyDataBroadcaster> relayedBroadcasters, long localPlayerId, HashSet<long> gridsQueued)
        {
            var sphere = new BoundingSphere(Entity.PositionComp.GetPosition(), 0.5f);

            MyRadioBroadcasters.GetAllBroadcastersInSphere(sphere, m_broadcastersInRange);

            foreach (var broadcaster in m_broadcastersInRange)
            {
                if (relayedBroadcasters.Add(broadcaster) == false)
                    continue;

                if (!CanIUseIt(broadcaster, localPlayerId))
                    continue;
                MyDataReceiver radioReceiver;
                if (broadcaster.Container.TryGet<MyDataReceiver>(out radioReceiver))
                {
                    radioReceiver.UpdateBroadcastersInRange(relayedBroadcasters, localPlayerId, gridsQueued);
                }
            }
        }
    }
}
