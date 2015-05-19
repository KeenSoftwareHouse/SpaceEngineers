#region Using

using ProtoBuf;
using System.Xml.Serialization;
using System;
using System.Collections.Generic;
using VRageMath;

#endregion

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiBlockCategoryDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public string Name;

        [ProtoMember]
        public string[] ItemIds;

        [ProtoMember]
        public bool IsShipCategory = false;

        [ProtoMember]
        public bool IsBlockCategory = true;

        [ProtoMember]
        public bool SearchBlocks = true;

        [ProtoMember]
        public bool ShowAnimations = false;

        [ProtoMember]
        public bool Public = true;
    }
}
