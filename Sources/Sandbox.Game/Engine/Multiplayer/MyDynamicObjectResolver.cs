using System;
using VRage.Library.Collections;
using VRage.Network;
using VRage.Serialization;

namespace Sandbox.Engine.Multiplayer
{
    public class MyDynamicObjectResolver : IDynamicResolver
    {
        public void Serialize(BitStream stream, Type baseType, ref Type obj)
        {
            if (stream.Reading)
            {
                var id = new TypeId(stream.ReadUInt32());

                obj = MyMultiplayer.Static.ReplicationLayer.GetType(id);
            }
            else
            {
                var id = MyMultiplayer.Static.ReplicationLayer.GetTypeId(obj);
                stream.WriteUInt32(id);
            }
        }
    }
}
