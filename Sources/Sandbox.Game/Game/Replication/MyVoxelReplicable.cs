using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Replication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game.Entity;
using VRage.Library.Collections;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Replication;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Game.Replication
{
    class MyVoxelReplicable : MyEntityReplicableBaseEvent<MyVoxelBase>, IMyStreamableReplicable
    {
        Action<MyVoxelBase> m_loadingDoneHandler;
        MyStreamingEntityStateGroup<MyVoxelReplicable> m_streamingGroup;

        public MyVoxelBase Voxel { get { return Instance; } }

        public override float GetPriority(MyClientInfo client)
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
            return GetBasePriority(Voxel.PositionComp.GetPosition(), Voxel.Storage.Size * MyVoxelConstants.VOXEL_SIZE_IN_METRES, client);
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
            bool rangeChanged = VRage.Serialization.MySerializer.CreateAndRead<bool>(stream);

            byte[] data = null;
            string asteroidName = null;
            if (rangeChanged)
            {
                data = VRage.Serialization.MySerializer.CreateAndRead<byte[]>(stream);
            }
            else if(isUserCreated)
            {
                asteroidName = VRage.Serialization.MySerializer.CreateAndRead<string>(stream);
            }

            if (isFromPrefab)
            {
                var builder = VRage.Serialization.MySerializer.CreateAndRead<MyObjectBuilder_EntityBase>(stream, MyObjectBuilderSerializer.Dynamic);

                if (rangeChanged && data != null)
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
                        MyEntities.Add(voxelMap);
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
                    MyEntities.Add(voxelMap);
                }
                else
                {
                    TryRemoveExistingEntity(builder.EntityId);

                    voxelMap = (MyVoxelBase)MyEntities.CreateFromObjectBuilderNoinit(builder);
                    voxelMap.Init(builder);
                    MyEntities.Add(voxelMap);
                }
            }
            else
            {
                long voxelMapId = VRage.Serialization.MySerializer.CreateAndRead<long>(stream);
                MyEntities.TryGetEntityById<MyVoxelBase>(voxelMapId, out voxelMap);
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
                m_streamingGroup = new MyStreamingEntityStateGroup<MyVoxelReplicable>(this);
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

            bool contentChanged = (Voxel.ContentChanged || (Voxel.CreatedByUser && Voxel.AsteroidName == null));
            VRage.Serialization.MySerializer.Write(stream, ref contentChanged);

            if (contentChanged)
            {
                byte[] data;
                Voxel.Storage.Save(out data);
                VRage.Serialization.MySerializer.Write(stream, ref data);

                Console.WriteLine(String.Format("sending content : {0}", data.Length));
            }
            else if (isUserCreated)
            {
                string asteroidName = Voxel.AsteroidName;
                VRage.Serialization.MySerializer.Write(stream, ref asteroidName);
            }

            if (isFromPrefab)
            {
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
    }
}
