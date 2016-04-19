using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Game.Components
{
    public abstract class MyIngameScript
    {
        public abstract void Init(VRage.Game.ModAPI.Ingame.IMyCubeBlock block);
    }
}
