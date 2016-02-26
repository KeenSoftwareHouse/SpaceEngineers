using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Entities.Blocks
{
    class MyLaserReceiver : MyDataReceiver
    {
        protected override void GetAllBroadcastersInMyRange(ref HashSet<MyDataBroadcaster> relayedBroadcasters, long localPlayerId, HashSet<long> gridsQueued)
        {
            
            (Entity as MyLaserAntenna).AddBroadcastersContactingMe(ref relayedBroadcasters);

            var broadcaster = (Entity as MyLaserAntenna).GetOthersBroadcaster();
            if (broadcaster != null)
            {
                if (relayedBroadcasters.Contains(broadcaster))
                    return;
                relayedBroadcasters.Add(broadcaster);
                if (CanIUseIt(broadcaster, localPlayerId))
                {
                    MyDataReceiver laserReceiver;
                    if (broadcaster.Container.TryGet<MyDataReceiver>(out laserReceiver))
                    {
                         laserReceiver.UpdateBroadcastersInRange(relayedBroadcasters, localPlayerId, gridsQueued);
                    }
                }
                    

            }
        }
    }
}
