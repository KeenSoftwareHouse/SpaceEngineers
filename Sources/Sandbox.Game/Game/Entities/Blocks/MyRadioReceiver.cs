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
        public MyRadioReceiver(MyEntity parent):base(parent)
        {
            Enabled = true;
        }

        protected override void GetAllBroadcastersInMyRange(ref HashSet<MyDataBroadcaster> relayedBroadcasters, long localPlayerId, HashSet<long> gridsQueued)
        {
            var sphere = new BoundingSphere(Parent.PositionComp.GetPosition(), 0.5f);

            MyRadioBroadcasters.GetAllBroadcastersInSphere(sphere, m_broadcastersInRange);

            foreach (var broadcaster in m_broadcastersInRange)
            {
                if (relayedBroadcasters.Contains(broadcaster))
                    continue;

                relayedBroadcasters.Add(broadcaster);

                if (!CanIUseIt(broadcaster, localPlayerId))
                    continue;

                if (broadcaster.Parent is IMyComponentOwner<MyDataReceiver>)
                {
                    MyDataReceiver radioReceiver;
                    if ((broadcaster.Parent as IMyComponentOwner<MyDataReceiver>).GetComponent(out radioReceiver))
                    {
                        radioReceiver.UpdateBroadcastersInRange(relayedBroadcasters, localPlayerId, gridsQueued);
                    }
                }
            }
        }
    }
}
