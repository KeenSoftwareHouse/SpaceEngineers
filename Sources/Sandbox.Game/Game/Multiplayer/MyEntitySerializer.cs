using VRage.Serialization;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game.Entity;

namespace Sandbox.Game.Multiplayer
{
    class MyEntitySerializer : ISerializer<MyEntity>
    {
        public static readonly MyEntitySerializer Default = new MyEntitySerializer();

        void ISerializer<MyEntity>.Serialize(ByteStream destination, ref MyEntity data)
        {
            long entityId = data.EntityId;
            BlitSerializer<long>.Default.Serialize(destination, ref entityId);
        }

        void ISerializer<MyEntity>.Deserialize(ByteStream source, out MyEntity data)
        {
            long entityId;
            BlitSerializer<long>.Default.Deserialize(source, out entityId);
            MyEntities.TryGetEntityById(entityId, out data);
        }
    }
}
