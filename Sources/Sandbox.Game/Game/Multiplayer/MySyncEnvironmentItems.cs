using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.EnvironmentItems;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game.Entity;
using VRage.Network;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Multiplayer
{
    [StaticEventOwner]
    public static class MySyncEnvironmentItems
    {
        public static Action<MyEntity, int> OnRemoveEnvironmentItem;

        public static void RemoveEnvironmentItem(long entityId, int itemInstanceId)
        {
            MyMultiplayer.RaiseStaticEvent(s => OnRemoveEnvironmentItemMessage, entityId, itemInstanceId);
        }

        [Event, Reliable, Server, BroadcastExcept]
        static void OnRemoveEnvironmentItemMessage(long entityId, int itemInstanceId)
        {
            MyEntity entity;
            if (MyEntities.TryGetEntityById(entityId, out entity))
            {
                if (OnRemoveEnvironmentItem != null)
                {
                    OnRemoveEnvironmentItem(entity, itemInstanceId);
                }
            }
            else if (MyFakes.ENABLE_FLORA_COMPONENT_DEBUG)
            {
                System.Diagnostics.Debug.Fail("Received OnRemoveEnvironmentItemMessage to remove environment item, but entity wasn't found!");
            }
        }

        public static void SendModifyModelMessage(long entityId, int instanceId, MyStringHash subtypeId)
        {
            MyMultiplayer.RaiseStaticEvent(s => OnModifyModelMessage, entityId, instanceId, subtypeId);
        }

        [Event, Reliable, Broadcast]
        static void OnModifyModelMessage(long entityId, int instanceId, MyStringHash subtypeId)
        {
            Debug.Assert(!Sync.IsServer);
            MyEnvironmentItems entity;
            if (MyEntities.TryGetEntityById<MyEnvironmentItems>(entityId, out entity))
            {
                entity.ModifyItemModel(instanceId, subtypeId, true, false);
            }
            else if (MyFakes.ENABLE_FLORA_COMPONENT_DEBUG)
            {
                System.Diagnostics.Debug.Fail("Received OnModifyModelMessage, but entity wasn't found!");
            }
        }


        public static void SendBeginBatchAddMessage(long entityId)
        {
            MyMultiplayer.RaiseStaticEvent(s => OnBeginBatchAddMessage, entityId);
        }

        [Event, Reliable, Broadcast]
        static void OnBeginBatchAddMessage(long entityId)
        {
            Debug.Assert(!Sync.IsServer);
            MyEnvironmentItems entity;
            if (MyEntities.TryGetEntityById<MyEnvironmentItems>(entityId, out entity))
            {
                entity.BeginBatch(false);
            }
        }

        public static void SendBatchAddItemMessage(long entityId, Vector3D position, MyStringHash subtypeId)
        {
            MyMultiplayer.RaiseStaticEvent(s => OnBatchAddItemMessage, entityId, position, subtypeId);
        }

        [Event, Reliable, Broadcast]
        static void OnBatchAddItemMessage(long entityId, Vector3D position, MyStringHash subtypeId)
        {
            Debug.Assert(!Sync.IsServer);
            MyEnvironmentItems entity;
            if (MyEntities.TryGetEntityById<MyEnvironmentItems>(entityId, out entity))
            {
                entity.BatchAddItem(position, subtypeId, false);
            }
        }

        public static void SendBatchModifyItemMessage(long entityId, int localId, MyStringHash subtypeId)
        {
            MyMultiplayer.RaiseStaticEvent(s => OnBatchModifyItemMessage, entityId, localId, subtypeId);
        }

        [Event, Reliable, Broadcast]
        static void OnBatchModifyItemMessage(long entityId, int localId, MyStringHash subtypeId)
        {
            Debug.Assert(!Sync.IsServer);
            MyEnvironmentItems entity;
            if (MyEntities.TryGetEntityById<MyEnvironmentItems>(entityId, out entity))
            {
                entity.BatchModifyItem(localId, subtypeId, false);
            }
        }

        public static void SendBatchRemoveItemMessage(long entityId, int localId)
        {
            MyMultiplayer.RaiseStaticEvent(s => OnBatchRemoveItemMessage, entityId, localId);
        }

        [Event, Reliable, Broadcast]
        static void OnBatchRemoveItemMessage(long entityId, int localId)
        {
            Debug.Assert(!Sync.IsServer);
            MyEnvironmentItems envItems;
            if (MyEntities.TryGetEntityById<MyEnvironmentItems>(entityId, out envItems))
            {
                envItems.BatchRemoveItem(localId, false);
            }
        }

        public static void SendEndBatchAddMessage(long entityId)
        {
            MyMultiplayer.RaiseStaticEvent(s => OnEndBatchAddMessage, entityId);
        }

        [Event, Reliable, Broadcast]
        static void OnEndBatchAddMessage(long entityId)
        {
            Debug.Assert(!Sync.IsServer);
            MyEnvironmentItems entity;
            if (MyEntities.TryGetEntityById<MyEnvironmentItems>(entityId, out entity))
            {
                entity.EndBatch(false);
            }
        }
    }
}
