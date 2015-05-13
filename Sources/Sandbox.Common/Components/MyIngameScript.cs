using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.Components
{
    public abstract class MyIngameScript
    {
        public abstract void Init(Sandbox.ModAPI.Ingame.IMyCubeBlock block);
    }
}
