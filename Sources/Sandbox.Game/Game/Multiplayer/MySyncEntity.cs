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
using VRage.Components;
using VRage.Library.Utils;
using Sandbox.Common;
using VRage.ModAPI;
using VRage.Library.Collections;
using Sandbox.Engine.Physics;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    public class MySyncEntity : MySyncComponentBase
    {
        const P2PMessageEnum PositionMessageEnum = MyFakes.UNRELIABLE_POSITION_SYNC ? P2PMessageEnum.Unreliable : P2PMessageEnum.Reliable;

        [MessageId(10, PositionMessageEnum)]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct PositionUpdateMsg : IEntityMessage
        {
            // 50.5 B Total
            public long EntityId; // 8 B

            public Vector3D Position; // 24 B
            public Quaternion Orientation; // 6.5 B

            public HalfVector3 LinearVelocity; // 6 B
            public HalfVector3 AngularVelocity; // 6 B

            public long GetEntityId() { return EntityId; }

            public override string ToString()
            {
                return String.Format("{0}, {1}, Velocity: {2}", this.GetType().Name, this.GetEntityText(), LinearVelocity.ToString());
            }
        }

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
            MySyncLayer.RegisterEntityMessage<MySyncEntity, ClosedMsg>(EntityClosedSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
        }

        private static readonly uint m_sleepTimeForRequest = 30;
        public byte DefaultUpdateCount = 4;
        public byte LinearMovementUpdateCount = 20;

        protected byte m_updateFrameCount; // Update position once every x frames
        protected bool m_isLocallyDirty = false;
        protected uint m_lastUpdateFrame = 0;
        protected uint m_stationaryUpdatesCount = 0;

        private Vector3D? m_lastServerPosition;
        private Quaternion? m_lastServerOrientation;

        public readonly new MyEntity Entity;

        public uint StationaryUpdatesCount
        {
            get { return m_stationaryUpdatesCount; }
        }

        public virtual bool IsMoving
        {
            get
            {
                // Never know if somebody is moving entity when physics is null
                return Entity.Physics == null
                    || Entity.Physics.LinearVelocity != Vector3.Zero
                    || Entity.Physics.AngularVelocity != Vector3.Zero;
            }
        }

        public virtual bool IsAccelerating
        {
            get
            {
                // Never know if somebody is moving entity when physics is null
                const float epsilonSq = 0.05f * 0.05f;
                return Entity.Physics == null
                    || Entity.Physics.LinearAcceleration.LengthSquared() > epsilonSq
                    || Entity.Physics.AngularAcceleration.LengthSquared() > epsilonSq;
            }
        }

        public MySyncEntity(MyEntity entity)
        {
            Entity = entity;
            m_lastUpdateFrame = MyMultiplayer.Static != null ? MyMultiplayer.Static.FrameCounter : 0;
            m_updateFrameCount = LinearMovementUpdateCount;
        }

        internal bool ShouldSendPhysicsUpdate()
        {
            m_updateFrameCount = IsAccelerating ? DefaultUpdateCount : LinearMovementUpdateCount;

            int updateFrameCount = MyFakes.NEW_POS_UPDATE_TIMING ? (int)Math.Round(m_updateFrameCount * MyPhysics.SimulationRatio) : m_updateFrameCount;
            return MyMultiplayer.Static.FrameCounter - m_lastUpdateFrame >= updateFrameCount;
        }

        internal void SetPhysicsUpdateSent()
        {
            if (IsMoving)
                m_stationaryUpdatesCount = 0;
            else
                m_stationaryUpdatesCount++;

            m_lastUpdateFrame = MyMultiplayer.Static.FrameCounter;
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
        /// Serializes sync entity physics, default implementation serializes position, orientation, linear and angular velocity.
        /// </summary>
        public virtual void SerializePhysics(BitStream stream, MyNetworkClient sender, bool highOrientationCompression = false)
        {
            PositionUpdateMsg msg = stream.Writing ? CreatePositionMsg(Entity) : default(PositionUpdateMsg);
            stream.Serialize(ref msg.Position); // 24B
            if (highOrientationCompression)
                stream.SerializeNormCompressed(ref msg.Orientation); // 29b
            else
                stream.SerializeNorm(ref msg.Orientation); // 52b
            stream.Serialize(ref msg.LinearVelocity); // 6B
            stream.Serialize(ref msg.AngularVelocity); // 6B
            if (stream.Reading)
            {
                OnPositionUpdate(ref msg, sender);
            }
        }

        internal static PositionUpdateMsg CreatePositionMsg(IMyEntity entity)
        {
            var m = entity.WorldMatrix;
            PositionUpdateMsg msg = new PositionUpdateMsg();
            msg.EntityId = entity.EntityId;
            msg.Orientation = Quaternion.CreateFromForwardUp(m.Forward, m.Up);
            msg.Position = m.Translation;
            if (entity.Physics != null)
            {
                if (MyPerGameSettings.EnableMultiplayerVelocityCompensation)
                {
                    float ratio = MathHelper.Clamp(Sandbox.Engine.Physics.MyPhysics.SimulationRatio, 0, 2);

                    msg.LinearVelocity = entity.Physics.LinearVelocity * ratio;
                    msg.AngularVelocity = entity.Physics.AngularVelocity * ratio;
                }
                else
                {
                    msg.LinearVelocity = entity.Physics.LinearVelocity;
                    msg.AngularVelocity = entity.Physics.AngularVelocity;
                }
            }
            return msg;
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

        internal bool ResponsibleForUpdate(MyNetworkClient player)
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

        internal virtual void OnPositionUpdate(ref PositionUpdateMsg msg, MyNetworkClient sender)
        {
            // Validate that client sending position update to server is reponsible for update
            if (Sync.IsServer && !ResponsibleForUpdate(sender))
                return;

            if (!Sync.IsServer && ResponsibleForUpdate(Sync.Clients.LocalClient))
                return;

            var q = msg.Orientation;
            var m = Matrix.CreateFromQuaternion(q);

            var world = MatrixD.CreateWorld(msg.Position, m.Forward, m.Up);

            Debug.Assert(Entity.PositionComp != null, "Entity doesn't not have position component");
            if (Entity.PositionComp != null)
                Entity.PositionComp.SetWorldMatrix(world, this);

            if (Entity.Physics != null)
            {
                Entity.Physics.LinearVelocity = msg.LinearVelocity;
                Entity.Physics.AngularVelocity = msg.AngularVelocity;

                if (MyPerGameSettings.EnableMultiplayerVelocityCompensation)
                {
                    float ratio = MathHelper.Clamp(Sandbox.Engine.Physics.MyPhysics.SimulationRatio, 0.1f, 2);

                    Entity.Physics.LinearVelocity /= ratio;
                    Entity.Physics.AngularVelocity /= ratio;
                }

                Entity.Physics.UpdateAccelerations();

                if (!Entity.Physics.IsMoving && Entity.Physics.RigidBody != null && Entity.Physics.RigidBody.IsAddedToWorld)
                {
                    Entity.Physics.RigidBody.Deactivate();
                }
            }

            if (Sync.IsServer)
            {
                MarkPhysicsDirty();
            }
            else
            {
                // Store last update from server
                m_isLocallyDirty = false;
                m_lastUpdateFrame = MyMultiplayer.Static.FrameCounter;
                m_lastServerPosition = msg.Position;
                m_lastServerOrientation = msg.Orientation;
            }
        }

        public override void SendCloseRequest()
        {
            var msg = new ClosedMsg();
            msg.EntityId = Entity.EntityId;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void EntityClosedRequest(MySyncEntity sync, ref ClosedMsg msg, MyNetworkClient sender)
        {
            // Test right to closing entity (e.g. is creative mode?)
            EntityClosedSuccess(sync, ref msg, Sync.Clients.LocalClient);
            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        static void EntityClosedSuccess(MySyncEntity sync, ref ClosedMsg msg, MyNetworkClient sender)
        {
            if (!sync.Entity.MarkedForClose)
                sync.Entity.Close();
        }

        public static void SendPositionUpdates(List<IMyEntity> entities)
        {
            throw new NotSupportedException();
        }
    }
}
