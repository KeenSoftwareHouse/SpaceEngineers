﻿using ProtoBuf;
using System.ComponentModel;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CargoContainer : MyObjectBuilder_TerminalBlock
    {
        [ProtoMember, DefaultValue(null)]
        public MyObjectBuilder_Inventory Inventory = null;

        [ProtoMember, DefaultValue(null)]
        public string ContainerType = null;
        public bool ShouldSerializeContainerType() { return ContainerType != null; }

        public MyObjectBuilder_CargoContainer()
        {

        }

        public override void SetupForProjector()
        {
            base.SetupForProjector();
            if (Inventory != null)
                Inventory.Clear();
        }
    }
}
