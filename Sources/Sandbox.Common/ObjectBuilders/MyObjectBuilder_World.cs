using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Serialization;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_World : MyObjectBuilder_Base
    {
        [ProtoMember(1)]
        public MyObjectBuilder_Checkpoint Checkpoint;

        [ProtoMember(2)]
        public MyObjectBuilder_Sector Sector;

        [ProtoMember(3)]
        public SerializableDictionary<string, byte[]> VoxelMaps;
    }
}
