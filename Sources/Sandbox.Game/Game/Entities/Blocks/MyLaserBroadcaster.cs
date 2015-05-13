using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Entities.Blocks
{
    public class MyLaserBroadcaster : MyDataBroadcaster
    {
        public MyLaserBroadcaster(MyEntity parent)
        {
            Parent = parent;
        }
    }
}
