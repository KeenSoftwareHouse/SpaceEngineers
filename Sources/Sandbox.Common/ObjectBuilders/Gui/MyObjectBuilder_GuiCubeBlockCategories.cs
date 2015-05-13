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
        [ProtoMember(1)]
        public string Name;

        [ProtoMember(2)]
        public string[] ItemIds;

        [ProtoMember(3)]
        public bool IsShipCategory = false;

        [ProtoMember(4)]
        public bool IsBlockCategory = true;

        [ProtoMember(5)]
        public bool SearchBlocks = true;

        [ProtoMember(6)]
        public bool ShowAnimations = false;

        [ProtoMember(7)]
        public bool Public = true;
    }
}
