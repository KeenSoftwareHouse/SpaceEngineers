#region Using

using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using SteamSDK;
using System;
using VRageMath;

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


        static MySyncManipulationTool()
        {
            MySyncLayer.RegisterMessage<StartManipulationMsg>(StartManipulationCallback, MyMessagePermissions.ToSelf|MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<StopManipulationMsg>(StopManipulationCallback, MyMessagePermissions.ToSelf | MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
        }

        long m_entityId;

        public MySyncManipulationTool(long entityId)
        {
            m_entityId = entityId;
        }

        public void StartManipulation(MyManipulationTool.MyState state, MyEntity otherEntity, Vector3D hitPosition, ref MatrixD ownerWorldHeadMatrix)
        {
            StartManipulationMsg msg = new StartManipulationMsg();
            msg.EntityId = m_entityId;
            msg.ToolState = state;
            msg.OtherEntity = otherEntity.EntityId;
            msg.HitPosition = hitPosition;
            msg.OwnerWorldHeadMatrix = ownerWorldHeadMatrix;

            MyManipulationTool manipulationTool;
            if (MyEntities.TryGetEntityById(msg.EntityId, out manipulationTool))
            {
                manipulationTool.StartManipulation(state, otherEntity, hitPosition, ref ownerWorldHeadMatrix);
                if (!Sync.IsServer && manipulationTool.IsHoldingItem)
                    MySession.Static.SyncLayer.SendMessageToServer(ref msg);
            }
        }

        static void StartManipulationCallback(ref StartManipulationMsg msg, MyNetworkClient sender)
        {
            MyManipulationTool manipulationTool;
            MyEntity otherEntity;
            if (MyEntities.TryGetEntityById(msg.EntityId, out manipulationTool) && MyEntities.TryGetEntityById(msg.OtherEntity, out otherEntity))
            {
                manipulationTool.StartManipulation(msg.ToolState, otherEntity, msg.HitPosition, ref msg.OwnerWorldHeadMatrix, true);
                if (Sync.IsServer)
                {
                    Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
                }
            }
        }

        public void StopManipulation()
        {
            MyManipulationTool manipulationTool;
            if (MyEntities.TryGetEntityById(m_entityId, out manipulationTool))
            {
                manipulationTool.StopManipulation();
            }
         
            StopManipulationMsg msg = new StopManipulationMsg();
            msg.EntityId = m_entityId;

            MySession.Static.SyncLayer.SendMessageToServer(ref msg);
        }

        static void StopManipulationCallback(ref StopManipulationMsg msg, MyNetworkClient sender)
        {
            MyManipulationTool manipulationTool;
            if (MyEntities.TryGetEntityById(msg.EntityId, out manipulationTool))
            {
                if (Sync.IsServer)
                {
                    Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
                }
                manipulationTool.StopManipulation();
            }
        }
    }
}
