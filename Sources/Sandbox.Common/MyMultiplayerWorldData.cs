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
        [ProtoMember(1)]
        public T Message;
    }

    [ProtoContract]
    [Obsolete]
    public class MyMultiplayerWorldData
    {
        //[ProtoMember(1)]
        //public MyObjectBuilder_World World;

        //[ProtoMember(2)]
        //public Dictionary<Type, ushort> TypeMap;

        //[ProtoMember(3)]
        //public SerializableDictionary<string, byte[]> VoxelMaps;
    }
}
