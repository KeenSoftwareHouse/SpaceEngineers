using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Replication;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Library.Collections;
using VRage.Library.Sync;
using VRage.Network;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Game.Replication
{
    class MyCharacterReplicable : MyEntityReplicableBaseEvent<MyCharacter>
    {
        private MyPropertySyncStateGroup m_propertySync;

        protected override IMyStateGroup CreatePhysicsGroup()
        {
            return new MyCharacterPhysicsStateGroup(Instance, this);
        }

        protected override void OnHook()
        {
            base.OnHook();
            m_propertySync = new MyPropertySyncStateGroup(this, Instance.SyncType);
        }

        public override float GetPriority(MyClientInfo state)
        {
            float priority = 0.0f;
            if(Instance == null || state.State == null)
            {
                return priority;
            }

            var player = state.State.GetPlayer();
            if (player != null && player.Character == Instance)
            {
                priority = 1.0f;
            }
            else
            {
                priority = base.GetPriority(state);
            }

            if (Instance.IsUsing is MyShipController)
            {
                if (priority < 0.01f)
                {
                    //force pilot to client wve when its too far away.
                    var parent = MyExternalReplicable.FindByObject((Instance.IsUsing as MyShipController).CubeGrid);

                    if (state.HasReplicable(parent))
                    {
                        priority = 1.0f;
                    }
                }
            }

            return priority;
        }

        public override void GetStateGroups(List<IMyStateGroup> resultList)
        {
            base.GetStateGroups(resultList);
            if (m_propertySync != null && m_propertySync.PropertyCount > 0)
                resultList.Add(m_propertySync);
        }

        public override IMyReplicable GetDependency()
        {
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
               
                Vector3 velocity = builder.LinearVelocity;
                velocity *= MyEntityPhysicsStateGroup.EffectiveSimulationRatio;
                builder.LinearVelocity = velocity;

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
               
                Vector3 velocity = builder.LinearVelocity;
                velocity /= MyEntityPhysicsStateGroup.EffectiveSimulationRatio;
                builder.LinearVelocity = velocity;

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
    }
}
