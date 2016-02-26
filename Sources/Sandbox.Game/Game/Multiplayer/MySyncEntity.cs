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

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    public class MySyncEntity : MySyncComponentBase
    {
        [MessageId(12, P2PMessageEnum.Reliable)]
        protected struct ClosedMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId() { return EntityId; }

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        static MySyncEntity()
        {
            MySyncLayer.RegisterEntityMessage<MySyncEntity, ClosedMsg>(EntityClosedRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
        }

        public readonly new MyEntity Entity;

        public MySyncEntity(MyEntity entity)
        {
            Entity = entity;
        }

        public override void MarkPhysicsDirty()
        {
            if (MyMultiplayer.Static == null)
                return;

            // Client with dirty physics (modified locally) and no physics update from server for 2x 30 frames
            //if (!Sync.IsServer && !ResponsibleForUpdate(this) && (MyMultiplayer.Static.FrameCounter - m_lastUpdateFrame) >= 120)
            //{
            //    if(!m_isLocallyDirty)
            //    {
            //        m_isLocallyDirty = true;
            //        m_lastUpdateFrame = MyMultiplayer.Static.FrameCounter;
            //    }
            //    else
            //    {
            //        // Use last position received from server
            //        if (m_lastServerOrientation.HasValue && m_lastServerPosition.HasValue)
            //        {
            //            var m = Matrix.CreateFromQuaternion(m_lastServerOrientation.Value);
            //            m.Translation = m_lastServerPosition.Value;
            //            Entity.WorldMatrix = m;
            //            m_isLocallyDirty = false;
            //            m_lastUpdateFrame = MyMultiplayer.Static.FrameCounter;
            //        }
            //    }
            //}
            MyMultiplayer.Static.MarkPhysicsDirty(this);
        }

        /// <summary>
        /// For direct calls by inherited classes
        /// </summary>
        protected static bool ResponsibleForUpdate(MySyncEntity entity)
        {
            return entity.ResponsibleForUpdate(Sync.Clients.LocalClient);
        }

        public ulong GetResponsiblePlayer()
        {
            var controllingPlayer = Sync.Players.GetControllingPlayer(Entity);
            return controllingPlayer != null ? controllingPlayer.Id.SteamId : Sync.ServerId;
        }

        public bool ResponsibleForUpdate(MyNetworkClient player)
        {
            if (Sync.Players == null)
                return false;

            var controllingPlayer = Sync.Players.GetControllingPlayer(Entity);
            if (controllingPlayer == null)
            {
                var character = Entity as MyCharacter;
                if (character != null && character.CurrentRemoteControl != null)
                {
                    controllingPlayer = Sync.Players.GetControllingPlayer(character.CurrentRemoteControl as MyEntity);
                }
            }

            if (controllingPlayer == null)
            {
                return player.IsGameServer();
            }
            else
            {
                return controllingPlayer.Client == player;
            }
        }

        public override void SendCloseRequest()
        {
            // TODO: This should be changed, only used for client-side entity close in special cases (e.g. cut operation of clipboard)
            var msg = new ClosedMsg();
            msg.EntityId = Entity.EntityId;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void EntityClosedRequest(MySyncEntity sync, ref ClosedMsg msg, MyNetworkClient sender)
        {
            // Test right to closing entity (e.g. is creative mode?)
            if (!sync.Entity.MarkedForClose)
                sync.Entity.Close(); // close only on server, server uses replication to propagate it to clients
        }
    }
}
