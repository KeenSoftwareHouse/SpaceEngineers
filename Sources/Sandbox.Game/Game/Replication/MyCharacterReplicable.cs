using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using SpaceEngineers.Game.SessionComponents;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Library.Collections;
using VRage.Network;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Game.Replication
{
    class MyCharacterReplicable : MyEntityReplicableBaseEvent<MyCharacter>
    {
        private StateGroups.MyPropertySyncStateGroup m_propertySync;
        //MyEntityPositionVerificationStateGroup m_posVerGroup;
        HashSet<IMyReplicable> m_dependencies = new HashSet<IMyReplicable>();

        protected override IMyStateGroup CreatePhysicsGroup()
        {
            //m_posVerGroup = new MyCharacterPositionVerificationStateGroup(Instance);
            return new StateGroups.MyCharacterPhysicsStateGroup(Instance, this);
        }

        protected override void OnHook()
        {
            base.OnHook();
            m_propertySync = new StateGroups.MyPropertySyncStateGroup(this, Instance.SyncType);
        }

        public override float GetPriority(MyClientInfo state,bool cached)
        {
            float priority = 0.0f;
            if(Instance == null || state.State == null)
            {
                return priority;
            }

            ulong clientEndpoint = state.EndpointId.Value;
            if (cached)
            {
                if (m_cachedPriorityForClient != null && m_cachedPriorityForClient.ContainsKey(clientEndpoint))
                {
                    return m_cachedPriorityForClient[clientEndpoint];
                }
            }

            if (m_cachedPriorityForClient == null)
            {
                m_cachedPriorityForClient = new Dictionary<ulong, float>();
            }

            var player = state.State.GetPlayer();
            if (player != null && player.Character == Instance)
            {
                priority = 1.0f;
            }
            else
            {
                //Sync all characters now, as they can serve as antenna relay
                priority = 0.1f;
                //priority = base.GetPriority(state, cached);
            }

            if (Instance.IsUsing is MyShipController)
            {
                //Pilot cannot have higher priority than the grid they control. Otherwise bugs ensue
                var parent = MyExternalReplicable.FindByObject((Instance.IsUsing as MyShipController).CubeGrid);

                if (state.HasReplicable(parent))
                {
                    priority = parent.GetPriority(state, cached);
                }
                else
                {
                    priority = 0.0f;
                }
            }

            if (MyFakes.MP_ISLANDS)
            {
                BoundingBoxD aabb;

                if (player.Character != null)
                {
                    if (MyIslandSyncComponent.Static.GetIslandAABBForEntity(player.Character, out aabb))
                    {
                        var ipriority = GetBasePriority(aabb.Center, aabb.Size, state);

                        MyIslandSyncComponent.Static.SetPriorityForIsland(player.Character, state.EndpointId.Value, ipriority);

                        return ipriority;
                    }
                }

            }


            m_cachedPriorityForClient[clientEndpoint] = priority;
            return priority;
        }

        public override void GetStateGroups(List<IMyStateGroup> resultList)
        {
            base.GetStateGroups(resultList);
            if (m_propertySync != null && m_propertySync.PropertyCount > 0)
            {
                resultList.Add(m_propertySync);
            }
            //resultList.Add(m_posVerGroup);
        }

        public override IMyReplicable GetParent()
        {
            if (Instance == null)
                return null;

            if (Instance.IsUsing is MyShipController)
            {
                return MyExternalReplicable.FindByObject((Instance.IsUsing as MyShipController).CubeGrid);
            }

            if (MyPerGameSettings.BlockForVoxels)
            {
                foreach (var voxelMap in MySession.Static.VoxelMaps.Instances)
                {
                    return MyExternalReplicable.FindByObject(voxelMap);
                }
            }
            return null;
        }

        public override bool OnSave(BitStream stream)
        {
            Debug.Assert(Instance != null, "Saving null replicable!");
            if (Instance == null)
                return false;

            stream.WriteBool(Instance.IsUsing is MyShipController);
            if (Instance.IsUsing is MyShipController)
            {
                long ownerId = Instance.IsUsing.EntityId;
                VRage.Serialization.MySerializer.Write(stream, ref ownerId);

                long characterId = Instance.EntityId;
                VRage.Serialization.MySerializer.Write(stream, ref characterId);
            }
            else
            {
                MyObjectBuilder_Character builder = (MyObjectBuilder_Character)Instance.GetObjectBuilder();
               
                VRage.Serialization.MySerializer.Write(stream, ref builder, MyObjectBuilderSerializer.Dynamic);
            }
            return true;
        }

        protected override void OnLoad(BitStream stream, Action<MyCharacter> loadingDoneHandler)
        {
            bool isUsing;
            VRage.Serialization.MySerializer.CreateAndRead(stream, out isUsing);

            if (isUsing)
            {
                long ownerId;
                VRage.Serialization.MySerializer.CreateAndRead(stream, out ownerId);
                long characterId;
                VRage.Serialization.MySerializer.CreateAndRead(stream, out characterId);
                MyEntities.CallAsync(() => LoadAsync(ownerId, characterId, loadingDoneHandler));
            }
            else
            {
                MyCharacter character = new MyCharacter();
                MyObjectBuilder_Character builder = (MyObjectBuilder_Character)VRage.Serialization.MySerializer.CreateAndRead<MyObjectBuilder_EntityBase>(stream, MyObjectBuilderSerializer.Dynamic);
                TryRemoveExistingEntity(builder.EntityId);
               
                MyEntities.InitAsync(character, builder, true, (e) => loadingDoneHandler(character));
            }
        }

        private static void LoadAsync(long ownerId, long characterId, Action<MyCharacter> loadingDoneHandler)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(ownerId, out entity);

            MyShipController owner = entity as MyShipController;
            if (owner != null)
            {
                if (owner.Pilot != null)
                {
                    loadingDoneHandler(owner.Pilot);
                    MySession.Static.Players.UpdatePlayerControllers(ownerId);
                }
                else
                {
                    MyEntity characterEntity;
                    MyEntities.TryGetEntityById(characterId, out characterEntity);

                    MyCharacter character = characterEntity as MyCharacter;
                    loadingDoneHandler(character);
                }
            }
            else
            {
                loadingDoneHandler(null);
            }
        }


        void TryRemoveExistingEntity(long entityId)
        {
            MyEntity oldEntity;
            if (MyEntities.TryGetEntityById(entityId, out oldEntity))
            {
                oldEntity.EntityId = MyEntityIdentifier.AllocateId();
                oldEntity.Close();
            }
        }

        public override HashSet<IMyReplicable> GetDependencies()
        {
            m_dependencies.Clear();

            MyPlayerCollection playerCollection = MySession.Static.Players;
            var connectedPlayers = playerCollection.GetOnlinePlayers();

            foreach (var player in connectedPlayers)
            {
                if (player.Character == Instance)
                {
                    var broadcasters = Instance.RadioReceiver.GetRelayedBroadcastersForPlayer(player.Identity.IdentityId);
                    foreach (var broadcaster in broadcasters)
                    {
                        IMyReplicable dep = MyExternalReplicable.FindByObject(broadcaster.Entity);
                        if (dep != null)
                        {
                            m_dependencies.Add(dep.GetParent() ?? dep);
                        }
                    }
                }
            }

            return m_dependencies;
        }
    }
}
