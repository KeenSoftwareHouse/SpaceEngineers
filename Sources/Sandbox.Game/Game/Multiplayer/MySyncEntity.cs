using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using VRageMath;
using SteamSDK;
using System.Diagnostics;
using Sandbox.Game.Entities.Character;
using System.Runtime.InteropServices;
using System.Linq.Expressions;
using VRageMath.PackedVector;
using VRage.Serialization;
using Sandbox.Engine.Utils;
using VRage;
using VRage.Game.Components;
using VRage.Library.Utils;
using Sandbox.Common;
using VRage.ModAPI;
using VRage.Library.Collections;
using Sandbox.Engine.Physics;
using VRage.Game.Entity;
using VRage.Network;

namespace Sandbox.Game.Multiplayer
{
    [StaticEventOwner]
    [PreloadRequired]
    public class MySyncEntity : MySyncComponentBase
    {
        public readonly new MyEntity Entity;

        public MySyncEntity(MyEntity entity)
        {
            Entity = entity;
        }

        public override void SendCloseRequest()
        {
            // TODO: This should be changed, only used for client-side entity close in special cases (e.g. cut operation of clipboard)
            MyMultiplayer.RaiseStaticEvent(s => MySyncEntity.OnEntityClosedRequest, Entity.EntityId);
        }

        [Event, Reliable, Server]
        static void OnEntityClosedRequest(long entityId)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(entityId, out entity);
            if (entity == null)
                return;

            // Test right to closing entity (e.g. is creative mode?)
            if (!entity.MarkedForClose)
                entity.Close(); // close only on server, server uses replication to propagate it to clients
        }
    }
}
