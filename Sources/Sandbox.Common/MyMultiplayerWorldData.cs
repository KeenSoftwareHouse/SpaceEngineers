using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using VRage.Serialization;

namespace Sandbox.Engine.Multiplayer
{
    /// <summary>
    /// Container class to prevent boxing in ProtoBuf
    /// </summary>
    [ProtoContract]
    public class Container<T>
        where T : struct
    {
        [ProtoMember]
        public T Message;
    }

    [ProtoContract]
    [Obsolete]
    public class MyMultiplayerWorldData
    {
        //[ProtoMember]
        //public MyObjectBuilder_World World;

        //[ProtoMember]
        //public Dictionary<Type, ushort> TypeMap;

        //[ProtoMember]
        //public SerializableDictionary<string, byte[]> VoxelMaps;
    }
}
