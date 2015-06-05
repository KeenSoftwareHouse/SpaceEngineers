using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ModAPI;
using VRage.Utils;

namespace VRage.Components
{
    public class MyEntityComponentContainer : MyComponentContainer<MyEntityComponentBase>
    {
        public IMyEntity Entity { get; private set; }

        public MyEntityComponentContainer(IMyEntity entity)
        {
            Entity = entity;
        }
	}
}
