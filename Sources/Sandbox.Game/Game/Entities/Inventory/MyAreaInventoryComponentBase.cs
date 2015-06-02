using Sandbox.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Components;

namespace Sandbox.Game
{
	public abstract class MyAreaInventoryComponentBase : MyComponentBase
	{
        public abstract IMyComponentInventory GetInventory();
	}
}
