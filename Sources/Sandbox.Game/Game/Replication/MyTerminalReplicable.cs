using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Replication;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Library.Collections;
using VRage.Network;
using VRage.Replication;
using VRage.Serialization;

namespace Sandbox.Game.Replication
{
    /// <summary>
    /// Responsible for synchronizing cube block properties over network
    /// </summary>
    class MyTerminalReplicable : MyExternalReplicableEvent<MySyncedBlock>
    {
        public MySyncedBlock Block { get { return Instance; } }

        private StateGroups.MyPropertySyncStateGroup m_propertySync;

        public MyTerminalReplicable() { }

        public override void OnServerReplicate()
        {
            base.OnServerReplicate();
            m_propertySync.MarkDirty();
        }

        protected override void OnHook()
        {
            Debug.Assert(MyMultiplayer.Static != null, "Should not get here without multiplayer");
            base.OnHook();
            m_propertySync = new StateGroups.MyPropertySyncStateGroup(this, Block.SyncType);
            m_propertySync.GlobalValidate = context => HasRights(context.ClientState.GetClient());
            Block.OnClose += (entity) => RaiseDestroyed();
        }

        public bool HasRights(MyNetworkClient client)
        {
            if(Block == null ||client == null || client.FirstPlayer == null || client.FirstPlayer.Identity == null)
            {
                return false;
            }

            var relationToBlockOwner = Block.GetUserRelationToOwner(client.FirstPlayer.Identity.IdentityId);
            return relationToBlockOwner == VRage.Game.MyRelationsBetweenPlayerAndBlock.FactionShare
                || relationToBlockOwner == VRage.Game.MyRelationsBetweenPlayerAndBlock.Owner
                || relationToBlockOwner == VRage.Game.MyRelationsBetweenPlayerAndBlock.NoOwnership;
        }

        public override IMyReplicable GetParent()
        {
            if (Block == null)
                return null;

            Debug.Assert(!Block.Closed && !Block.CubeGrid.Closed, "Sending terminal replicable on closed block/grid");
            return FindByObject(Block.CubeGrid); 
        }

        public override float GetPriority(MyClientInfo client, bool cached)
        {
            Debug.Fail("Getting priority of child replicable: MyTerminalReplicable");
            return 0; // This is child replicable
        }

        public override bool OnSave(BitStream stream)
        {
            stream.WriteInt64(Block.EntityId);

            return true;
        }

        MySyncedBlock FindBlock(long blockEntityId)
        {
            MySyncedBlock result;
            MyEntities.TryGetEntityById<MySyncedBlock>(blockEntityId, out result);
         //   Debug.Assert(result != null, "[INFO] Block for terminal replicable not found");
            return result;
        }

        protected override void OnLoad(BitStream stream, Action<MySyncedBlock> loadingDoneHandler)
        {
            long blockEntityId = stream.ReadInt64();
            MyEntities.CallAsync(() => loadingDoneHandler(FindBlock(blockEntityId)));
        }

        protected override void OnLoadBegin(BitStream stream, Action<MySyncedBlock> loadingDoneHandler)
        {
            OnLoad(stream, loadingDoneHandler);
        }

        public override void OnDestroy()
        {
        }

        public override void GetStateGroups(List<IMyStateGroup> resultList)
        {
            resultList.Add(m_propertySync);
        }

        public override bool HasToBeChild
        {
            get { return true; }
        }

        public override VRageMath.BoundingBoxD GetAABB()
        {
            System.Diagnostics.Debug.Fail("GetAABB can be called only on root replicables");
            return VRageMath.BoundingBoxD.CreateInvalid();
        }
    }
}
