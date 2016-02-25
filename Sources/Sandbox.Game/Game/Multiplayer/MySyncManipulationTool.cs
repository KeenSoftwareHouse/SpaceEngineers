#region Using

using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using SteamSDK;
using System;
using VRageMath;
using Sandbox.Engine.Networking;
using VRage.Game.Entity;
using System.Diagnostics;

#endregion

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    class MySyncManipulationTool
    {
        [MessageId(4185, P2PMessageEnum.Reliable)]
        struct StartManipulationMsg
        {
            public long EntityId;
            public MyManipulationTool.MyState ToolState;
            public long OtherEntity;
            public Vector3D HitPosition;
            public MatrixD OwnerWorldHeadMatrix;
        }

        [MessageId(4186, P2PMessageEnum.Reliable)]
        struct StopManipulationMsg
        {
            public long EntityId;
        }

        [MessageId(4187, P2PMessageEnum.Reliable)]
        struct RotateManipulatedEntityMsg
        {
            public long EntityId;
            public Quaternion Rotation;
        }


        static MySyncManipulationTool()
        {
            MySyncLayer.RegisterMessage<StartManipulationMsg>(StartManipulationCallback, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer | MyMessagePermissions.ToSelf, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<StopManipulationMsg>(StopManipulationCallback, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer | MyMessagePermissions.ToSelf, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<RotateManipulatedEntityMsg>(RotateManipulatedEntityCallback, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer | MyMessagePermissions.ToSelf, MyTransportMessageEnum.Request);
        }

        long m_entityId;

        public MySyncManipulationTool(long entityId)
        {
            m_entityId = entityId;
        }

        public void StartManipulation(MyManipulationTool.MyState state, MyEntity otherEntity, Vector3D hitPosition, ref MatrixD ownerWorldHeadMatrix)
        {
            Debug.Assert(Sync.IsServer);
            if (!Sync.IsServer)
                return;

            StartManipulationMsg msg = new StartManipulationMsg();
            msg.EntityId = m_entityId;
            msg.ToolState = state;
            msg.OtherEntity = otherEntity.EntityId;
            msg.HitPosition = hitPosition;
            msg.OwnerWorldHeadMatrix = ownerWorldHeadMatrix;

            MyManipulationTool manipulationTool;
            if (MyEntities.TryGetEntityById(msg.EntityId, out manipulationTool))
            {
                bool manipulationStarted = manipulationTool.StartManipulation(state, otherEntity, hitPosition, ref ownerWorldHeadMatrix);
                if (manipulationStarted && manipulationTool.IsHoldingItem)
                {
                    MySession.Static.SyncLayer.SendMessageToAll(ref msg);
                }
            }
        }

        static void StartManipulationCallback(ref StartManipulationMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(!Sync.IsServer);

            MyManipulationTool manipulationTool;
            MyEntity otherEntity;
            if (MyEntities.TryGetEntityById(msg.EntityId, out manipulationTool) && MyEntities.TryGetEntityById(msg.OtherEntity, out otherEntity))
            {
                manipulationTool.StartManipulation(msg.ToolState, otherEntity, msg.HitPosition, ref msg.OwnerWorldHeadMatrix, true);
            }
        }

        public void StopManipulation()
        {
            // Can be called from Client!
            // Stop manipulation immediately (to avoid sending StopMAnipulation again and again form clients)
            MyManipulationTool manipulationTool;
            if (MyEntities.TryGetEntityById(m_entityId, out manipulationTool))
            {
                manipulationTool.StopManipulation();
            }

            StopManipulationMsg msg = new StopManipulationMsg();
            msg.EntityId = m_entityId;

            if (Sync.IsServer)
                MySession.Static.SyncLayer.SendMessageToAll(ref msg);
            else
                MySession.Static.SyncLayer.SendMessageToServer(ref msg);
        }

        static void StopManipulationCallback(ref StopManipulationMsg msg, MyNetworkClient sender)
        {
            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
            }

            MyManipulationTool manipulationTool;
            if (MyEntities.TryGetEntityById(msg.EntityId, out manipulationTool))
            {
                manipulationTool.StopManipulation();
            }
        }

        public void RotateManipulatedEntity(Quaternion rotation)
        {
            RotateManipulatedEntityMsg msg = new RotateManipulatedEntityMsg();
            msg.EntityId = m_entityId;
            msg.Rotation = rotation;

            if (Sync.IsServer)
                MySession.Static.SyncLayer.SendMessageToAll(ref msg);
            else
                MySession.Static.SyncLayer.SendMessageToServer(ref msg);
        }

        static void RotateManipulatedEntityCallback(ref RotateManipulatedEntityMsg msg, MyNetworkClient sender)
        {
            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
            }

            MyManipulationTool manipulationTool;
            if (MyEntities.TryGetEntityById(msg.EntityId, out manipulationTool))
            {
                manipulationTool.RotateManipulatedEntity(ref msg.Rotation);
            }
        }

    }
}
