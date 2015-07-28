﻿using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.ComponentSystem
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ComponentContainer : MyObjectBuilder_Base
    {
        [ProtoContract]
        public class ComponentData
        {
            [ProtoMember]
            public string TypeId;

            [ProtoMember]
            public MyObjectBuilder_ComponentBase Component;
        }

        [ProtoMember]
        public List<ComponentData> Components = new List<ComponentData>();
    }
}
