using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Entities.Blocks
{
    public class MyLaserReceiver : MyDataReceiver
    {
        public MyLaserReceiver(MyEntity parent):base(parent)
        {

        }

        protected override void GetAllBroadcastersInMyRange(ref HashSet<MyDataBroadcaster> relayedBroadcasters, long localPlayerId, HashSet<long> gridsQueued)
        {
            
            (Parent as MyLaserAntenna).AddBroadcastersContactingMe(ref relayedBroadcasters);
            
            var broadcaster = (Parent as MyLaserAntenna).GetOthersBroadcaster();
            if (broadcaster != null)
            {
                if (relayedBroadcasters.Contains(broadcaster))
                    return;
                relayedBroadcasters.Add(broadcaster);
                if (CanIUseIt(broadcaster, localPlayerId))
                {
                    MyDataReceiver laserReceiver;
                    if ((broadcaster.Parent as IMyComponentOwner<MyDataReceiver>).GetComponent(out laserReceiver))
                    {
                         laserReceiver.UpdateBroadcastersInRange(relayedBroadcasters, localPlayerId, gridsQueued);
                    }
                }
                    

            }
        }
    }
}
