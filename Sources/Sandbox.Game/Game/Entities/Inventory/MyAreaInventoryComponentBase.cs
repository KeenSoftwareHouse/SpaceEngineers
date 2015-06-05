using Sandbox.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Components;
using VRage.Utils;

namespace Sandbox.Game
{
    public abstract class MyAreaInventoryComponentBase : MyEntityComponentBase
	{
        public static MyStringId ComponentName = MyStringId.GetOrCompute("AreaInventory");

        public override MyStringId Name
        {
            get { return ComponentName; }
        }

        public abstract IMyComponentInventory GetInventory();
	}
}
