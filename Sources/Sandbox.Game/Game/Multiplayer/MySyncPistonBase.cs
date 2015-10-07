using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    class MySyncPistonBase : MySyncCubeBlock
    {

        [MessageId(324, P2PMessageEnum.Reliable)]
        struct AttachMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId()
            {
                return EntityId;
            }

            public long TopEntityId;
        }

        [MessageId(325, P2PMessageEnum.Reliable)]
        struct VelocityMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId()
            {
                return EntityId;
            }

            public float Velocity;
        }

        [MessageId(326, P2PMessageEnum.Reliable)]
        struct MinMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId()
            {
                return EntityId;
            }

            public float Min;
        }

        [MessageId(327, P2PMessageEnum.Reliable)]
        struct MaxMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId()
            {
                return EntityId;
            }

            public float Max;
        }

        [MessageId(328, SteamSDK.P2PMessageEnum.Unreliable)]
        struct CurrentPositionMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId()
            {
                return EntityId;
            }

            public float CurrentPosition;
        }

        static MySyncPistonBase()
        {
            MySyncLayer.RegisterEntityMessage<MySyncPistonBase, VelocityMsg>(OnSetVelocity, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer | MyMessagePermissions.ToSelf);
            MySyncLayer.RegisterEntityMessage<MySyncPistonBase, MinMsg>(OnSetMin, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer);
            MySyncLayer.RegisterEntityMessage<MySyncPistonBase, MaxMsg>(OnSetMax, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer);
            MySyncLayer.RegisterEntityMessage<MySyncPistonBase, AttachMsg>(OnAttach, MyMessagePermissions.FromServer|MyMessagePermissions.ToServer);
            MySyncLayer.RegisterEntityMessage<MySyncPistonBase, CurrentPositionMsg>(OnSetCurrentPosition, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer);
        }

        public new MyPistonBase Entity
        {
            get { return (MyPistonBase)base.Entity; }
        }

        public Action<float> SyncPosition;

        public MySyncPistonBase(MyPistonBase block)
            :base(block)
        {
        }

        public void SetVelocity(float v)
        {
            var msg = new VelocityMsg();
            msg.EntityId = Entity.EntityId;
            msg.Velocity = v;

            OnSetVelocity(this, ref msg, Sync.Clients.LocalClient);
            Sync.Layer.SendMessageToServerAndSelf(ref msg);
        }

        public void SetMin(float v)
        {
            var msg = new MinMsg();
            msg.EntityId = Entity.EntityId;
            msg.Min = v;

            OnSetMin(this, ref msg, Sync.Clients.LocalClient);
            Sync.Layer.SendMessageToServer(ref msg);
        }

        public void SetMax(float v)
        {
            var msg = new MaxMsg();
            msg.EntityId = Entity.EntityId;
            msg.Max = v;

            OnSetMax(this, ref msg, Sync.Clients.LocalClient);
            Sync.Layer.SendMessageToServer(ref msg);
        }

        static void OnSetVelocity(MySyncPistonBase sync, ref VelocityMsg msg, MyNetworkClient sender)
        {
            sync.Entity.Velocity = msg.Velocity;
            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
            }
        }

        static void OnSetMin(MySyncPistonBase sync, ref MinMsg msg, MyNetworkClient sender)
        {
            sync.Entity.MinLimit = msg.Min;
            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
            }
        }

        static void OnSetMax(MySyncPistonBase sync, ref MaxMsg msg, MyNetworkClient sender)
        {
            sync.Entity.MaxLimit = msg.Max;
            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
            }
        }

        internal void AttachTop(MyPistonTop topBlock)
        {
            var msg = new AttachMsg();
            Debug.Assert(Entity != null, "null entity when attaching piston top !");
            Debug.Assert(topBlock != null, "null topBlock when attaching piston top !");

            if (Entity != null && topBlock != null)
            {
                msg.EntityId = Entity.EntityId;
                msg.TopEntityId = topBlock.EntityId;

                Sync.Layer.SendMessageToServer(ref msg);
            }
            else
            {
                MySandboxGame.Log.WriteLine(string.Format("Failed to attach piston top ! Entity value :  {0} top block value : {1}",Entity,topBlock));
            }
        }

        private static void OnAttach(MySyncPistonBase sync, ref AttachMsg msg, MyNetworkClient sender)
        {
            MyPistonBase pistonBase = (MyPistonBase)sync.Entity;
            MyEntity rotorEntity = null;
            if (!MyEntities.TryGetEntityById(msg.TopEntityId, out rotorEntity))
            {
                pistonBase.RetryAttach(msg.TopEntityId);
                Debug.Assert(false, "Could not find top entity to attach to base");
                return;
            }
            MyPistonTop top = (MyPistonTop)rotorEntity;

            Debug.Assert(pistonBase.CubeGrid != top.CubeGrid, "Trying to attach top to base on the same grid");

            if (top.CubeGrid.InScene == false)
            {
                pistonBase.RetryAttach(msg.TopEntityId);
            }
            else
            {
                pistonBase.Attach(top, false);
            }

            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
            }
        }

        internal void SetCurrentPosition(float m_currentPos)
        {
            var msg = new CurrentPositionMsg();
            msg.EntityId = Entity.EntityId;
            msg.CurrentPosition = m_currentPos;

            Sync.Layer.SendMessageToServer<CurrentPositionMsg>(ref msg);
        }

        static void OnSetCurrentPosition(MySyncPistonBase sync, ref CurrentPositionMsg msg, MyNetworkClient sender)
        {
            var grid = sync.Entity.CubeGrid;
            if (grid.Physics == null || grid.MarkedForClose || sync.Entity.MarkedForClose)
                return;
            sync.SyncPosition(msg.CurrentPosition);

            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
            }
        }
    }
}
