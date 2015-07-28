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

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    public class MySyncEntity : MySyncComponentBase
    {
        [MessageId(10, P2PMessageEnum.Reliable)]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct PositionUpdateMsg : IEntityMessage
        {
            // 52 B Total
            public long EntityId; // 8 B

            public Vector3D Position; // 24 B
            public HalfVector4 Orientation; // 8 B

            public HalfVector3 LinearVelocity; // 6 B
            public HalfVector3 AngularVelocity; // 6 B

            public long GetEntityId() { return EntityId; }

            public override string ToString()
            {
                return String.Format("{0}, {1}, Velocity: {2}", this.GetType().Name, this.GetEntityText(), LinearVelocity.ToString());
            }
        }

        [MessageId(5741, P2PMessageEnum.Reliable)]
        internal struct PositionUpdateBatchMsg
        {
            public List<PositionUpdateMsg> Positions;
        }

        class PositionUpdateBatchSerializer : ISerializer<PositionUpdateBatchMsg>
        {
            void ISerializer<PositionUpdateBatchMsg>.Serialize(ByteStream destination, ref PositionUpdateBatchMsg data)
            {
                destination.Write7BitEncodedInt(data.Positions.Count);
                for (int i = 0; i < data.Positions.Count; i++)
                {
                    var msg = data.Positions[i];
                    BlitSerializer<PositionUpdateMsg>.Default.Serialize(destination, ref msg);
                }
            }

            void ISerializer<PositionUpdateBatchMsg>.Deserialize(ByteStream source, out PositionUpdateBatchMsg data)
            {
                data = new PositionUpdateBatchMsg();
                data.Positions = new List<PositionUpdateMsg>();

                int count = source.Read7BitEncodedInt();
                for (int i = 0; i < count; i++)
                {
                    PositionUpdateMsg msg;
                    BlitSerializer<PositionUpdateMsg>.Default.Deserialize(source, out msg);
                    data.Positions.Add(msg);
                }
            }
        }

        [MessageId(11, P2PMessageEnum.Reliable)]
        protected struct RequestPositionUpdateMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId() { return EntityId; }

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
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

        class InterpolationHelper
        {
            public MatrixD CurrentMatrix;
            public MatrixD TargetMatrix;
            public float Time = 1;
        }


        static MySyncEntity()
        {
            MySyncLayer.RegisterMessage<PositionUpdateBatchMsg>(OnPositionBatchUpdate, MyMessagePermissions.Any, MyTransportMessageEnum.Request, new PositionUpdateBatchSerializer());
            MySyncLayer.RegisterEntityMessage<MySyncEntity, PositionUpdateMsg>(UpdateCallback, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncEntity, RequestPositionUpdateMsg>(RequestUpdateCallback, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncEntity, ClosedMsg>(EntityClosedRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterEntityMessage<MySyncEntity, ClosedMsg>(EntityClosedSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
        }

        private static readonly uint m_sleepTimeForRequest = 60;
        public byte DefaultUpdateCount = 4;
        public byte ConstantMovementUpdateCount = 20;

        protected byte m_updateFrameCount; // Update position once every x frames
        protected uint m_lastUpdateFrame = 0;
        protected MyTimeSpan m_lastUpdateTime;

        private bool m_positionDirty = false;

        public readonly new MyEntity Entity;

        public override bool UpdatesOnlyOnServer { get; set; }

        InterpolationHelper m_interpolator = new InterpolationHelper();

        public MySyncEntity(MyEntity entity)
        {
            Entity = entity;
            ResetUpdateTimer();
            UpdatesOnlyOnServer = false;
            m_updateFrameCount = ConstantMovementUpdateCount;
        }

        public override void UpdatePosition()
        {
            if (Entity.SyncFlag)
            {
                if (!IsResponsibleForUpdate)
                {
                    RequestPositionUpdate(); // Requests position update from owner, when moving entity is not updated by anyone

                    m_interpolator.CurrentMatrix = Entity.WorldMatrix;
                }
                else
                {
                    SendPositionUpdate();
                }
            }
        }

        private void RequestPositionUpdate()
        {
            if (MyMultiplayer.Static == null)
                return;
            Debug.Assert(MySandboxGame.Static != null);
            // This mechanic is here when entity starts moving by actions of local player which are not transfered to server
            // Local player is now moving entity, but owner does not update it and sending updates. This requests updates.

            bool timeForUpdate;
            if (MyFakes.NEW_POS_UPDATE_TIMING)
                timeForUpdate = (MySandboxGame.Static.UpdateTime - m_lastUpdateTime).Seconds > (m_sleepTimeForRequest / MyEngineConstants.UPDATE_STEPS_PER_SECOND);
            else
                timeForUpdate = MyMultiplayer.Static.FrameCounter - m_lastUpdateFrame >= m_sleepTimeForRequest;

            if (timeForUpdate)
            {
                // Request owner update
                var reqMsg = new RequestPositionUpdateMsg();
                reqMsg.EntityId = Entity.EntityId;
                if (false)
                {
                    MySession.Static.SyncLayer.SendMessageToServer(ref reqMsg);
                }
                else
                {
                    MySession.Static.SyncLayer.SendMessage(ref reqMsg, GetResponsiblePlayer());
                }
                ResetUpdateTimer();
            }
        }

        private void SendPositionUpdate()
        {
            if(MyMultiplayer.Static == null)
                return;
            Debug.Assert(MySandboxGame.Static != null);
            float epsilonSq = 0.05f * 0.05f;
            if (m_updateFrameCount == ConstantMovementUpdateCount && (Entity.Physics == null
                || Entity.Physics.LinearAcceleration.LengthSquared() > epsilonSq
                || Entity.Physics.AngularAcceleration.LengthSquared() > epsilonSq))
            {
                m_updateFrameCount = DefaultUpdateCount;
            }

            bool timeForUpdate;
            if (MyFakes.NEW_POS_UPDATE_TIMING)
                timeForUpdate = (MySandboxGame.Static.UpdateTime - m_lastUpdateTime).Seconds > (m_updateFrameCount / MyEngineConstants.UPDATE_STEPS_PER_SECOND);
            else
                timeForUpdate = MyMultiplayer.Static.FrameCounter - m_lastUpdateFrame >= m_updateFrameCount;

            if (timeForUpdate)
            {
                m_updateFrameCount = ConstantMovementUpdateCount;

                // TODO: abstraction would be nice
                var syncGrid = this as MySyncGrid;
                if (syncGrid != null)
                {
                    var g = MyCubeGridGroups.Static.Physical.GetGroup(syncGrid.Entity);

                    PositionUpdateBatchMsg msg = new PositionUpdateBatchMsg();
                    msg.Positions = new List<PositionUpdateMsg>(g.Nodes.Count);

                    foreach (var node in g.Nodes)
                    {
                        msg.Positions.Add(CreatePositionMsg(node.NodeData));
                        node.NodeData.SyncObject.ResetUpdateTimer();
                        node.NodeData.SyncObject.m_positionDirty = false;
                    }

                    MySession.Static.SyncLayer.SendMessageToAll(ref msg);
                }
                else
                {
                    ResetUpdateTimer();
                    PositionUpdateMsg msg = CreatePositionMsg(Entity);
                    MySession.Static.SyncLayer.SendMessageToAll(ref msg);
                }
                m_positionDirty = false;
            }
            else if (MyMultiplayer.Static != null)
            {
                MyMultiplayer.Static.RegisterForTick(this);
                m_positionDirty = true;
            }
        }

        public static void SendPositionUpdates(List<IMyEntity> entities)
        {
            PositionUpdateBatchMsg msg = new PositionUpdateBatchMsg();
            msg.Positions = new List<PositionUpdateMsg>(entities.Count);

            foreach (var entity in entities)
            {
                msg.Positions.Add(CreatePositionMsg(entity));
                ((MySyncEntity)entity.SyncObject).ResetUpdateTimer();
                ((MySyncEntity)entity.SyncObject).m_positionDirty = false;
            }

            MySession.Static.SyncLayer.SendMessageToAll(ref msg);
        }

        private static PositionUpdateMsg CreatePositionMsg(IMyEntity entity)
        {
            var m = entity.WorldMatrix;
            PositionUpdateMsg msg = new PositionUpdateMsg();
            msg.EntityId = entity.EntityId;
            msg.Orientation = new HalfVector4(Quaternion.CreateFromForwardUp(m.Forward, m.Up).ToVector4());
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

        public override void Tick()
        {
            if (!Entity.MarkedForClose)
            {
                if (m_positionDirty)
                    SendPositionUpdate();

                //if (m_interpolator.Time < 1)
                //{
                //    m_interpolator.Time += 0.1f;

                //    var targetMatrix = m_interpolator.TargetMatrix;
                //    var targetRotation = Quaternion.CreateFromRotationMatrix(targetMatrix);

                //    var currentMatrix = m_interpolator.CurrentMatrix;
                //    var currentRotation = Quaternion.CreateFromRotationMatrix(currentMatrix);

                //    var interpolatedRotation = Quaternion.Slerp(currentRotation, targetRotation, m_interpolator.Time);
                //    var interpolatedMatrix = Matrix.CreateFromQuaternion(interpolatedRotation);

                //    interpolatedMatrix.Translation = currentMatrix.Translation;

                //    Entity.SetWorldMatrix(interpolatedMatrix, this);

                //    Sync.Multiplayer.RegisterForTick(this);
                //}
            }
        }

        private void ResetUpdateTimer()
        {
            if (MyMultiplayer.Static != null)
            {
                if(MyFakes.NEW_POS_UPDATE_TIMING)
                    m_lastUpdateTime = MySandboxGame.Static.UpdateTime;
                m_lastUpdateFrame = MyMultiplayer.Static.FrameCounter;
            }
        }

        /// <summary>
        /// For direct calls by inherited classes
        /// </summary>
        protected static bool ResponsibleForUpdate(MySyncEntity entity)
        {
            return entity.IsResponsibleForUpdate;
        }

        private ulong GetResponsiblePlayer()
        {
            var controllingPlayer = Sync.Players.GetControllingPlayer(Entity);
            return controllingPlayer != null && !UpdatesOnlyOnServer ? controllingPlayer.Id.SteamId : Sync.ServerId;
        }

        internal bool ResponsibleForUpdate(MyNetworkClient player)
        {
            if (UpdatesOnlyOnServer)
                return player.IsGameServer();

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

        private bool IsResponsibleForUpdate
        {
            get
            {
                return ResponsibleForUpdate(Sync.Clients.LocalClient);
            }
        }

        static void OnPositionBatchUpdate(ref PositionUpdateBatchMsg msg, MyNetworkClient sender)
        {
            for (int i = 0; i < msg.Positions.Count; i++)
            {
                var m = msg.Positions[i];

                MyEntity e;
                if (MyEntities.TryGetEntityById<MyEntity>(m.EntityId, out e))
                {
                    (e.SyncObject as MySyncEntity).OnPositionUpdate(ref m, sender);
                }
            }
        }

        internal virtual void OnPositionUpdate(ref PositionUpdateMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(false == false, "When interpolation enabled, this should not be called");
            if (!ResponsibleForUpdate(sender))
            {
                // This happens when server just accepted state change (e.g. enter cockpit), but some messages about character position will come eventually from client
                return;
            }

            ResetUpdateTimer();

            var q = Quaternion.FromVector4(msg.Orientation.ToVector4());
            var m = Matrix.CreateFromQuaternion(q);

            m_interpolator.TargetMatrix = MatrixD.CreateWorld(msg.Position, m.Forward, m.Up);
            m_interpolator.CurrentMatrix = Entity.WorldMatrix;
            m_interpolator.Time = 0;

            MyMultiplayer.Static.RegisterForTick(this);

            Debug.Assert(Entity.PositionComp != null, "Entity doesn't not have position component");
            if (Entity.PositionComp != null)
                Entity.PositionComp.SetWorldMatrix(m_interpolator.TargetMatrix, this);

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

                if(!Entity.Physics.IsMoving && Entity.Physics.RigidBody != null && Entity.Physics.RigidBody.IsAddedToWorld)
                {
                    Entity.Physics.RigidBody.Deactivate();
                }
            }
        }

        public override void SendCloseRequest()
        {
            var msg = new ClosedMsg();
            msg.EntityId = Entity.EntityId;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void UpdateCallback(MySyncEntity sync, ref PositionUpdateMsg msg, MyNetworkClient sender)
        {
            sync.OnPositionUpdate(ref msg, sender);
        }

        static void RequestUpdateCallback(MySyncEntity sync, ref RequestPositionUpdateMsg msg, MyNetworkClient sender)
        {
            if (false && Sync.IsServer || sync.IsResponsibleForUpdate)
            {
                sync.UpdatePosition();
            }
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
    }
}
