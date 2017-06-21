using Sandbox.Common;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Profiler;

namespace Sandbox.Game.Entities
{
    public abstract class MyDataReceiver : MyEntityComponentBase
    {
        protected List<MyDataBroadcaster> m_broadcastersInRange = new List<MyDataBroadcaster>();
        protected HashSet<MyDataBroadcaster> m_relayedBroadcasters = new HashSet<MyDataBroadcaster>();
        //This is used by dedicated server
        protected HashSet<MyDataBroadcaster> m_relayedBroadcastersForPlayer = new HashSet<MyDataBroadcaster>();
        public bool Enabled { get; set; }

        HashSet<MyEntity> m_entitiesOnHud = new HashSet<MyEntity>();

        public HashSet<MyDataBroadcaster> RelayedBroadcasters
        {
            get
            {
                return m_relayedBroadcasters;
            }
        }

        public static HashSet<MyDataReceiver> GetGridRadioReceivers(MyCubeGrid grid)
        {
            return GetGridRadioReceivers(grid, MySession.Static.LocalPlayerId);
        }

        //Returns radio receivers on <grid> and any logicaly connected grids <playerId> has acces to
        public static HashSet<MyDataReceiver> GetGridRadioReceivers(MyCubeGrid grid, long playerId)
        {
            HashSet<MyDataReceiver> output = new HashSet<MyDataReceiver>();

            var group = MyCubeGridGroups.Static.Logical.GetGroup(grid);
            if (group == null)
            {
                CollectRecieversFromGrid(grid, playerId, output);
            }
            else
            {
                foreach (var node in group.Nodes)
                {
                    CollectRecieversFromGrid(node.NodeData, playerId, output);
                }
            }
            return output;
        }

        //Collects data recievers from single grid <playerId> has acces to
        private static void CollectRecieversFromGrid(MyCubeGrid grid, long playerId, HashSet<MyDataReceiver> output)
        {
            foreach (var block in grid.GetFatBlocks())
            {
                MyDataReceiver receiver;
                if (block.Components.TryGet<MyDataReceiver>(out receiver))
                {
                    MyIDModule module;
                    if ((block as IMyComponentOwner<MyIDModule>).GetComponent(out module))
                    {
                        //AB in broadcast window I need to access to all antenas in range
                        //if ((block as MyTerminalBlock).HasPlayerAccess(playerId) && module.Owner != 0)
                            output.Add(receiver);
                    }
                }
            }
        }

        protected HashSet<long> m_relayerGrids = new HashSet<long>();
        public void UpdateBroadcastersInRange()
        {
            m_relayedBroadcasters.Clear();
            m_relayerGrids.Clear();
            UpdateBroadcastersInRange(m_relayedBroadcasters, MySession.Static.LocalPlayerId, m_relayerGrids);
        }

        public HashSet<MyDataBroadcaster> GetRelayedBroadcastersForPlayer(long localPlayerId)
        {
            m_relayedBroadcastersForPlayer.Clear();
            m_relayerGrids.Clear();
            UpdateBroadcastersInRange(m_relayedBroadcastersForPlayer, localPlayerId, m_relayerGrids);
            return m_relayedBroadcastersForPlayer;
        }

        public void UpdateBroadcastersInRange(HashSet<MyDataBroadcaster> relayedBroadcasters, long localPlayerId, HashSet<long> gridsQueued)
        {
            m_broadcastersInRange.Clear();

            if (!MyFakes.ENABLE_RADIO_HUD || !Enabled)
                return;

            ProfilerShort.Begin("Relay");
            MyDataBroadcaster radioBroadcaster;
            if (Entity.Components.TryGet<MyDataBroadcaster>(out radioBroadcaster))
            {
                relayedBroadcasters.Add(radioBroadcaster);
            }
            ProfilerShort.End();

            ProfilerShort.Begin("UpdateBroadcasters");
            //add all from same grid:
            MyCubeGrid grid = Entity.GetTopMostParent() as MyCubeGrid;
            if (grid != null && !gridsQueued.Contains(grid.EntityId)) //astronaut has no grid
            {
                gridsQueued.Add(grid.EntityId);
                foreach (var block in grid.GetFatBlocks())
                {
                    MyDataBroadcaster broadcaster;
                    if (block.Components.TryGet<MyDataBroadcaster>(out broadcaster))
                    {
                        MyIDModule module;
                        if ((block as IMyComponentOwner<MyIDModule>).GetComponent(out module))
                        {
                            if ((block as MyTerminalBlock).HasPlayerAccess(localPlayerId) && module.Owner != 0
                                && !relayedBroadcasters.Contains(broadcaster))
                            {
                                relayedBroadcasters.Add(broadcaster);
                                if (!CanIUseIt(broadcaster, localPlayerId))
                                    continue;
                                MyDataReceiver radioReceiver;
                                if (broadcaster.Container.TryGet<MyDataReceiver>(out radioReceiver))
                                {
                                    ProfilerShort.Begin("UpdateReceiver");
                                    radioReceiver.UpdateBroadcastersInRange(relayedBroadcasters, localPlayerId, gridsQueued);
                                    ProfilerShort.End();

                                }
                            }
                        }
                    }
                }
            }
            ProfilerShort.End();

            ProfilerShort.Begin("GetAllBroadcastersInMyRange");
            GetAllBroadcastersInMyRange(ref relayedBroadcasters, localPlayerId, gridsQueued);
            ProfilerShort.End();
        }

        abstract protected void GetAllBroadcastersInMyRange(ref HashSet<MyDataBroadcaster> relayedBroadcasters, long localPlayerId, HashSet<long> gridsQueued);

        public bool CanIUseIt(MyDataBroadcaster broadcaster, long localPlayerId)
        {
            if (broadcaster.Entity is IMyComponentOwner<MyIDModule>)
            {
                MyIDModule broadcasterId;
                if ((broadcaster.Entity as IMyComponentOwner<MyIDModule>).GetComponent(out broadcasterId))
                {
                    VRage.Game.MyRelationsBetweenPlayerAndBlock relation = broadcasterId.GetUserRelationToOwner(localPlayerId);
                    if (relation == VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies || relation == VRage.Game.MyRelationsBetweenPlayerAndBlock.Neutral || broadcasterId.Owner == 0)
                        return false;
                }
            }
            if (broadcaster.Entity is MyCharacter)
            {
                var relation = (broadcaster.Entity as MyCharacter).GetRelationTo(localPlayerId);
                if (relation == VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies || relation == VRage.Game.MyRelationsBetweenPlayerAndBlock.Neutral)
                    return false;
            }
            return true;
        }


        public void UpdateHud(bool showMyself = false)
        {
            if (MySandboxGame.IsDedicated || MyHud.MinimalHud || MyHud.CutsceneHud)
                return;

            Clear();

            foreach (var broadcaster in m_relayedBroadcasters)
            {
                MyEntity entity = (MyEntity)broadcaster.Entity;
                if (entity != null)
                {
                    //Also ignore entity if it is preview entity or else it will update hud
                    if (entity.GetTopMostParent() is MyCubeGrid && entity.GetTopMostParent().IsPreview)
                        continue;

                    if (!showMyself && entity == Entity)
                        continue; //do not show myself

                    bool friendly = true;
                    if (broadcaster.Entity is IMyComponentOwner<MyIDModule>)
                    {
                        MyIDModule broadcasterId;
                        if ((broadcaster.Entity as IMyComponentOwner<MyIDModule>).GetComponent(out broadcasterId))
                        {
                            VRage.Game.MyRelationsBetweenPlayerAndBlock relation = broadcasterId.GetUserRelationToOwner(MySession.Static.LocalPlayerId);
                            if (relation == VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies || relation == VRage.Game.MyRelationsBetweenPlayerAndBlock.Neutral)
                                friendly = false;
                        }
                    }

                    MyLaserAntenna la = broadcaster.Entity as MyLaserAntenna;
                    if (la != null && la.ShowOnHUD == false)
                        continue;

                    foreach (var hudParams in entity.GetHudParams(friendly))
                    {
                        MyEntity hudParamsEntity = hudParams.Entity as MyEntity;
                        if (!m_entitiesOnHud.Contains(hudParamsEntity))
                        {
                            m_entitiesOnHud.Add(hudParamsEntity);
                            if (hudParams.BlinkingTime > 0)
                                MyHud.HackingMarkers.RegisterMarker(hudParamsEntity, hudParams);
                            else
                                if (!MyHud.HackingMarkers.MarkerEntities.ContainsKey(hudParamsEntity))
                                    MyHud.LocationMarkers.RegisterMarker(hudParamsEntity, hudParams);
                        }
                    }
                }
            }

            //manually draw markers for players that are out of range or have broadcasting turned off
            if (MySession.Static.AdminSettings.HasFlag(AdminSettingsEnum.ShowPlayers))
            {
                foreach (var player in MySession.Static.Players.GetOnlinePlayers())
                {
                    MyCharacter character = player.Character;
                    if (character == null)
                        continue;
                    
                    var hudParams = character.GetHudParams(false);
                    foreach (var param in hudParams)
                    {
                        MyEntity hudEntity = (MyEntity)param.Entity;
                        if (m_entitiesOnHud.Contains(hudEntity))
                            continue;

                        m_entitiesOnHud.Add(hudEntity);

                        MyHud.LocationMarkers.RegisterMarker(hudEntity, param);
                    }
                }
            }
        }


        public void Clear()
        {
            foreach (var entity in m_entitiesOnHud)
            {
                MyHud.LocationMarkers.UnregisterMarker(entity);
            }
            m_entitiesOnHud.Clear();
            m_broadcastersInRange.Clear();
        }


        public override string ComponentTypeDebugString
        {
            get { return "MyDataReciever"; }
        }
    }
}
