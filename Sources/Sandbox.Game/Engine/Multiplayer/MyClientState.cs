using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Replication;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game.Entity;
using VRage.Library.Collections;
using VRage.Network;
using VRageMath;

namespace Sandbox.Engine.Multiplayer
{
    /// <summary>
    /// Client state, can be defined per-game.
    /// </summary>
    public abstract class MyClientState : MyClientStateBase
    {
        public enum MyContextKind
        {
            None = 0,
            Terminal = 1,
            Inventory = 2,
            Production = 3,
        }

        /// <summary>
        /// Client point of interest, used on server to replicate nearby entities
        /// </summary>
        public Vector3D Position { get; protected set; }
        public MyContextKind Context { get; protected set; }
        public MyEntity ContextEntity { get; protected set; }

        public override void Serialize(BitStream stream)
        {
            if (stream.Writing)
                Write(stream);
            else
                Read(stream);
        }

        protected virtual MyEntity GetControlledEntity()
        {
            if (MySession.Static.HasAdminRights && MySession.Static.CameraController == MySpectatorCameraController.Static
                && MySpectatorCameraController.Static.SpectatorCameraMovement == MySpectatorCameraMovementEnum.UserControlled)
                return null;

            return MySession.Static.ControlledEntity != null ? MySession.Static.ControlledEntity.Entity.GetTopMostParent() : null;
        }

        private void Write(BitStream stream)
        {
            // TODO: Make sure sleeping, server controlled entities are not moving locally (or they can be but eventually their position should be corrected)
            MyEntity controlledEntity = GetControlledEntity();

            WriteShared(stream, controlledEntity);
            if (controlledEntity != null)
            {
                WriteInternal(stream, controlledEntity);
                WritePhysics(stream, controlledEntity);
            }
        }

        private void Read(BitStream stream)
        {
            MyNetworkClient sender;
            if (!Sync.Clients.TryGetClient(EndpointId.Value, out sender))
            {
                Debug.Fail("Unknown sender");
                return;
            }

            MyEntity controlledEntity;
            ReadShared(stream, sender, out controlledEntity);
            if (controlledEntity != null)
            {
                ReadInternal(stream, sender, controlledEntity);
                ReadPhysics(stream, sender, controlledEntity);
            }
        }

        protected abstract void WriteInternal(BitStream stream, MyEntity controlledEntity);
        protected abstract void ReadInternal(BitStream stream, MyNetworkClient sender, MyEntity controlledEntity);

        /// <summary>
        /// Shared area for SE and ME. So far it writes whether you have a controlled entity or not. In the latter case you get the spectator position
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="validControlledEntity"></param>
        private void WriteShared(BitStream stream, MyEntity controlledEntity)
        {
            stream.WriteBool(controlledEntity != null);
            if (controlledEntity == null)
            {
                Vector3D pos = MySpectatorCameraController.Static.Position;
                stream.Serialize(ref pos);
            }
            else
            {
                stream.WriteInt64(controlledEntity.EntityId);
            }
        }

        private void ReadShared(BitStream stream, MyNetworkClient sender, out MyEntity controlledEntity)
        {
            controlledEntity = null;

            var hasControlledEntity = stream.ReadBool();
            if (!hasControlledEntity)
            {
                Vector3D pos = Vector3D.Zero;
                stream.Serialize(ref pos); // 24B
                Position = pos;
            }
            else
            {
                var entityId = stream.ReadInt64();
                MyEntity entity;
                if (!MyEntities.TryGetEntityById(entityId, out entity))
                    return;

                Position = entity.WorldMatrix.Translation;

                // TODO: Obsolete check?
                MySyncEntity syncEntity = entity.SyncObject as MySyncEntity;
                if (syncEntity == null)
                    return;
                controlledEntity = entity;
            }
        }

        private void WritePhysics(BitStream stream, MyEntity controlledEntity)
        {
            IMyReplicable player = MyExternalReplicable.FindByObject(controlledEntity);
            if (player == null)
            {
                stream.WriteBool(false);
                return;
            }

            var stateGroup = player.FindStateGroup<MyEntityPhysicsStateGroup>();
            if (stateGroup == null)
            {

                stream.WriteBool(false);
                return;
            }
            bool isResponsible = stateGroup.ResponsibleForUpdate(new EndpointId(Sync.MyId));
            stream.WriteBool(isResponsible);
            if (isResponsible)
            {
                stateGroup.Serialize(stream, null, 0, 65535);
            }
        }

        private void ReadPhysics(BitStream stream, MyNetworkClient sender, MyEntity controlledEntity)
        {
            var stateGroup = MyExternalReplicable.FindByObject(controlledEntity).FindStateGroup<MyEntityPhysicsStateGroup>();
            bool hasPhysics = stream.ReadBool();
            if (hasPhysics && stateGroup.ResponsibleForUpdate(new EndpointId(sender.SteamUserId)))
            {
                stateGroup.Serialize(stream, null, 0, 65535);
            }
        }
    }
}
