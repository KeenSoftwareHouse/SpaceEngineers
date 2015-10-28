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

namespace Sandbox.Game.Entities
{
    abstract class MyDataReceiver
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

        public MyEntity Parent { get; protected set; }

        public MyDataReceiver(MyEntity parent)
        {
            Parent = parent;
        }

        public static HashSet<MyDataReceiver> GetGridRadioReceivers(MyCubeGrid grid)
        {
            return GetGridRadioReceivers(grid, MySession.LocalPlayerId);
        }

        public static HashSet<MyDataReceiver> GetGridRadioReceivers(MyCubeGrid grid, long playerId)
        {
            HashSet<MyDataReceiver> output = new HashSet<MyDataReceiver>();
            foreach (var block in grid.GetFatBlocks<MyCubeBlock>())
            {
                if (block is IMyComponentOwner<MyDataReceiver>)
                {
                    MyDataReceiver receiver;
                    if ((block as IMyComponentOwner<MyDataReceiver>).GetComponent(out receiver))
                    {
                        MyIDModule module;
                        if ((block as IMyComponentOwner<MyIDModule>).GetComponent(out module))
                        {
                            if ((block as MyTerminalBlock).HasPlayerAccess(playerId) && module.Owner != 0)
                                output.Add(receiver);
                        }
                    }
                }
            }
            return output;
        }

        protected HashSet<long> m_relayerGrids = new HashSet<long>();
        public void UpdateBroadcastersInRange()
        {
            m_relayedBroadcasters.Clear();
            m_relayerGrids.Clear();
            UpdateBroadcastersInRange(m_relayedBroadcasters, MySession.LocalPlayerId, m_relayerGrids);
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

            if (Parent is IMyComponentOwner<MyDataBroadcaster>)
            {
                MyDataBroadcaster radioBroadcaster;
                if ((Parent as IMyComponentOwner<MyDataBroadcaster>).GetComponent(out radioBroadcaster))
                {
                    relayedBroadcasters.Add(radioBroadcaster);
                }
            }
            
            //add all from same grid:
            MyCubeGrid grid=Parent.GetTopMostParent() as MyCubeGrid;
            if (grid != null //astronaut has no grid
                    && !gridsQueued.Contains(grid.EntityId))
            {
                gridsQueued.Add(grid.EntityId);
                foreach (var block in grid.GetFatBlocks<MyCubeBlock>())
                {
                    if (block is IMyComponentOwner<MyDataBroadcaster>)
                    {
                        MyDataBroadcaster broadcaster;
                        if ((block as IMyComponentOwner<MyDataBroadcaster>).GetComponent(out broadcaster))
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
                                    if (broadcaster.Parent is IMyComponentOwner<MyDataReceiver>)
                                    {
                                        MyDataReceiver radioReceiver;
                                        if ((broadcaster.Parent as IMyComponentOwner<MyDataReceiver>).GetComponent(out radioReceiver))
                                        {
                                            radioReceiver.UpdateBroadcastersInRange(relayedBroadcasters, localPlayerId, gridsQueued);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //
            GetAllBroadcastersInMyRange(ref relayedBroadcasters, localPlayerId, gridsQueued);
        }
        abstract protected void GetAllBroadcastersInMyRange(ref HashSet<MyDataBroadcaster> relayedBroadcasters, long localPlayerId, HashSet<long> gridsQueued);

        public bool CanIUseIt(MyDataBroadcaster broadcaster, long localPlayerId)
        {
            if (broadcaster.Parent is IMyComponentOwner<MyIDModule>)
            {
                MyIDModule broadcasterId;
                if ((broadcaster.Parent as IMyComponentOwner<MyIDModule>).GetComponent(out broadcasterId))
                {
                    MyRelationsBetweenPlayerAndBlock relation = broadcasterId.GetUserRelationToOwner(localPlayerId);
                    if (relation == MyRelationsBetweenPlayerAndBlock.Enemies || relation == MyRelationsBetweenPlayerAndBlock.Neutral || broadcasterId.Owner == 0)
                        return false;
                }
            }
            if (broadcaster.Parent is MyCharacter)
            {
                var relation = (broadcaster.Parent as MyCharacter).GetRelationTo(localPlayerId);
                if (relation == MyRelationsBetweenPlayerAndBlock.Enemies || relation == MyRelationsBetweenPlayerAndBlock.Neutral)
                    return false;
            }
            return true;
        }


        public void UpdateHud(bool showMyself = false)
        {
            if (MySandboxGame.IsDedicated || MyHud.MinimalHud)
                return;

            Clear();

            foreach (var broadcaster in m_relayedBroadcasters)
            {
                MyEntity entity = null;
                if (MyEntities.TryGetEntityById(broadcaster.EntityId, out entity))
                {
                    if (!showMyself && entity == Parent)
                        continue; //do not show myself

                    bool friendly = true;
                    if (broadcaster.Parent is IMyComponentOwner<MyIDModule>)
                    {
                        MyIDModule broadcasterId;
                        if ((broadcaster.Parent as IMyComponentOwner<MyIDModule>).GetComponent(out broadcasterId))
                        {
                            MyRelationsBetweenPlayerAndBlock relation = broadcasterId.GetUserRelationToOwner(MySession.LocalPlayerId);
                            if (relation == MyRelationsBetweenPlayerAndBlock.Enemies || relation == MyRelationsBetweenPlayerAndBlock.Neutral)
                                friendly = false;
                        }
                    }

                    MyLaserAntenna la = broadcaster.Parent as MyLaserAntenna;
                    if (la != null && la.ShowOnHUD == false)
                        continue;

                    foreach (var hudParams in entity.GetHudParams(friendly))
                    {
                        if (!m_entitiesOnHud.Contains(hudParams.Entity))
                        {
                            m_entitiesOnHud.Add(hudParams.Entity);
                            if (hudParams.BlinkingTime > 0)
                                MyHud.HackingMarkers.RegisterMarker(hudParams.Entity, hudParams);
                            else
                                if (!MyHud.HackingMarkers.MarkerEntities.ContainsKey(hudParams.Entity))
                                    MyHud.LocationMarkers.RegisterMarker(hudParams.Entity, hudParams);
                        }
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

    }
}
