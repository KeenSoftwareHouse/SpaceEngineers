﻿using ProtoBuf;
using System;
using VRageMath;
using System.Collections.Generic;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Gui
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiControls : MyObjectBuilder_Base
    {
        [ProtoMember]
        public List<MyObjectBuilder_GuiControlBase> Controls;
    }
}
