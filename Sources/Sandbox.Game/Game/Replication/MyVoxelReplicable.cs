using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Game.Entity;
using VRage.Library.Collections;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Game.Replication
{
    class MyVoxelReplicable : MyEntityReplicableBaseEvent<MyVoxelBase>, IMyStreamableReplicable
    {
        List<MyEntity> m_entities;
        Action<MyVoxelBase> m_loadingDoneHandler;
        StateGroups.MyStreamingEntityStateGroup<MyVoxelReplicable> m_streamingGroup;

        public MyVoxelBase Voxel { get { return Instance; } }

        public override float GetPriority(MyClientInfo client,bool cached)
        {
            if(Voxel == null || Voxel.Storage == null || Voxel.Closed)
            {
                return 0.0f;
            }
            if(Voxel is MyPlanet)
            {
                return 1.0f;
            }
            if (Voxel.Save == false && Voxel.ContentChanged == false && Voxel.BeforeContentChanged == false)
            {
                return 0.0f;
            }

            ulong clientEndpoint = client.EndpointId.Value;
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
            m_cachedPriorityForClient[clientEndpoint] = GetBasePriority(Voxel.PositionComp.GetPosition(), Voxel.Storage.Size * MyVoxelConstants.VOXEL_SIZE_IN_METRES, client);
          
            return m_cachedPriorityForClient[clientEndpoint];
        }

        public override bool OnSave(BitStream stream)
        {
            return false;     
        }

        protected override void OnLoad(BitStream stream, Action<MyVoxelBase> loadingDoneHandler)
        {
            MyVoxelBase voxelMap;

            bool isUserCreated = VRage.Serialization.MySerializer.CreateAndRead<bool>(stream);
            bool isFromPrefab = VRage.Serialization.MySerializer.CreateAndRead<bool>(stream);
            bool contentChanged = VRage.Serialization.MySerializer.CreateAndRead<bool>(stream);

            byte[] data = null;
            string asteroidName = null;
            if (contentChanged)
            {
                data = VRage.Serialization.MySerializer.CreateAndRead<byte[]>(stream);
            }
            else if(isUserCreated)
            {
                asteroidName = VRage.Serialization.MySerializer.CreateAndRead<string>(stream);
            }

            MyLog.Default.WriteLine("MyVoxelReplicable.OnLoad - isUserCreated:" + isUserCreated + " isFromPrefab:" + isFromPrefab + " contentChanged:" + contentChanged + " data?: " + (data != null).ToString());

            if (isFromPrefab)
            {
                var builder = VRage.Serialization.MySerializer.CreateAndRead<MyObjectBuilder_EntityBase>(stream, MyObjectBuilderSerializer.Dynamic);

                if (contentChanged && data != null)
                {
                    IMyStorage storage = MyStorageBase.Load(data);

                    if (MyEntities.TryGetEntityById<MyVoxelBase>(builder.EntityId, out voxelMap))
                    {
                        if(voxelMap is MyVoxelMap)
                        {
                            ((MyVoxelMap)voxelMap).Storage = storage;
                        }
                        else if(voxelMap is MyPlanet)
                        {
                            ((MyPlanet)voxelMap).Storage = storage;
                        }
                        else
                        {
                            Debug.Fail("Unknown voxel kind");
                        }
                    }
                    else
                    {
                        voxelMap = (MyVoxelBase)MyEntities.CreateFromObjectBuilderNoinit(builder);
                        if(voxelMap is MyVoxelMap)
                        {
                            ((MyVoxelMap)voxelMap).Init(builder, storage);
                        }
                        else if(voxelMap is MyPlanet)
                        {
                            ((MyPlanet)voxelMap).Init(builder, storage);
                        }
                        else
                        {
                            Debug.Fail("Unknown voxel kind");
                        }
                        if (voxelMap != null)
                        {
                            MyEntities.Add(voxelMap);
                        }
                    }
                   
                }
                else if (isUserCreated)
                {
                    TryRemoveExistingEntity(builder.EntityId);

                    IMyStorage storage = MyGuiScreenDebugSpawnMenu.CreateAsteroidStorage(asteroidName, 0);
                    voxelMap = (MyVoxelBase)MyEntities.CreateFromObjectBuilderNoinit(builder);
                    if (voxelMap is MyVoxelMap)
                    {
                        ((MyVoxelMap)voxelMap).Init(builder, storage);
                    }
                    if (voxelMap != null)
                    {
                        MyEntities.Add(voxelMap);
                    }
                }
                else
                {
                    TryRemoveExistingEntity(builder.EntityId);

                    voxelMap = (MyVoxelBase)MyEntities.CreateFromObjectBuilderNoinit(builder);  
                    if (voxelMap != null)
                    {
                        voxelMap.Init(builder);
                        MyEntities.Add(voxelMap);
                    }
                }
            }
            else
            {
                long voxelMapId = VRage.Serialization.MySerializer.CreateAndRead<long>(stream);
                MyEntities.TryGetEntityById<MyVoxelBase>(voxelMapId, out voxelMap);
            }

            if (voxelMap != null)
            {
                BoundingBoxD voxelBox = new BoundingBoxD(voxelMap.PositionLeftBottomCorner, voxelMap.PositionLeftBottomCorner + voxelMap.SizeInMetres);
                m_entities = MyEntities.GetEntitiesInAABB(ref voxelBox);
                foreach (var entity in m_entities)
                {
                    MyVoxelBase voxel = entity as MyVoxelBase;
                    if (voxel != null)
                    {
                        if (voxel.Save == false && voxel != voxelMap)
                        {
                            voxel.Close();
                            break;
                        }
                    }
                }
                m_entities.Clear();
            }
   
            loadingDoneHandler(voxelMap);
        }

        void TryRemoveExistingEntity(long entityId)
        {
            MyEntity oldEntity;
            if (MyEntities.TryGetEntityById(entityId, out oldEntity))
            {
                Debug.Fail("Adding voxel through replication: Entity with that id already exists!");
                oldEntity.EntityId = MyEntityIdentifier.AllocateId();
                oldEntity.Close();
            }
        }

        public override void OnDestroy()
        {
            if (Voxel != null && Voxel.Save)
            {
                Voxel.Close();
            }
        }

        public override void GetStateGroups(List<IMyStateGroup> resultList)
        {
           
        }

        protected override void OnLoadBegin(BitStream stream, Action<MyVoxelBase> loadingDoneHandler)
        {
            m_loadingDoneHandler = loadingDoneHandler;
        }

        public IMyStateGroup GetStreamingStateGroup()
        {
            if (m_streamingGroup == null)
            {
                m_streamingGroup = new StateGroups.MyStreamingEntityStateGroup<MyVoxelReplicable>(this, this);
            }
            return m_streamingGroup;
        }

        public void Serialize(BitStream stream)
        {
            int startStreamPosition = stream.BitPosition;
            bool isUserCreated = (Voxel.CreatedByUser && Voxel.AsteroidName != null);
            VRage.Serialization.MySerializer.Write(stream, ref isUserCreated);

            bool isFromPrefab = Voxel.Save;
            VRage.Serialization.MySerializer.Write(stream, ref isFromPrefab);

            bool contentChanged = (Voxel.ContentChanged || Voxel.BeforeContentChanged || (Voxel.CreatedByUser && Voxel.AsteroidName == null));
            VRage.Serialization.MySerializer.Write(stream, ref contentChanged);

            if (contentChanged)
            {
                byte[] data;
                Voxel.Storage.Save(out data);
                VRage.Serialization.MySerializer.Write(stream, ref data);

                if (!VRage.Game.MyFinalBuildConstants.IS_OFFICIAL)
                    Console.WriteLine(String.Format("sending content : {0}", data.Length));
            }
            else if (isUserCreated)
            {
                string asteroidName = Voxel.AsteroidName;
                VRage.Serialization.MySerializer.Write(stream, ref asteroidName);
            }

            if (isFromPrefab)
            {
                if (!VRage.Game.MyFinalBuildConstants.IS_OFFICIAL)
                    Console.WriteLine("voxel from prefab / saved");
                var builder = (MyObjectBuilder_EntityBase)Voxel.GetObjectBuilder();
                VRage.Serialization.MySerializer.Write(stream, ref builder, MyObjectBuilderSerializer.Dynamic);
            }
            else
            {
                long entityId = Voxel.EntityId;
                VRage.Serialization.MySerializer.Write(stream, ref entityId);
            }    
        }

        public void LoadDone(BitStream stream)
        {
            OnLoad(stream, m_loadingDoneHandler);
        }

        public float PriorityScale()
        {
            return  MyPerGameSettings.BlockForVoxels ? 10.0f : Voxel is MyPlanet ? 50.0f :  0.5f;
        }


        public bool NeedsToBeStreamed
        {
            get { return true; }
        }


        public void LoadCancel()
        {
            m_loadingDoneHandler(null);
        }

        public override VRageMath.BoundingBoxD GetAABB()
        {
            //We want detect planets and asteroids from larger distance
            var aabb = Instance.PositionComp.WorldAABB;

            if (Voxel is MyPlanet)
                aabb.Inflate((Voxel as MyPlanet).MaximumRadius * 50);
            else
                aabb.Inflate(Voxel.SizeInMetres.Length() * 50);
            return aabb;

            return Instance.PositionComp.WorldAABB;
        }
    }
}
