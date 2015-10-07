using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Replicables;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Replication;
using VRage.Voxels;
using VRageMath;


namespace Sandbox.Game.Entities
{
    class MyVoxelReplicable : MyEntityReplicableBaseEvent<MyVoxelBase>
    {
        public MyVoxelBase Voxel { get { return Instance; } }

        #region IMyReplicable Implementation
        public override float GetPriority(MyClientStateBase client)
        {
            if (Voxel.Save == false && Voxel.ContentChanged == false && Voxel.BeforeContentChanged == false)
            {
                return 0.0f;
            }
            return GetBasePriority(Voxel.PositionComp.GetPosition(), Voxel.Storage.Size * MyVoxelConstants.VOXEL_SIZE_IN_METRES, client);
        }

        public override void OnSave(BitStream stream)
        {
            bool isFromPrefab = Voxel.Save;
            VRage.Serialization.MySerializer.Write(stream, ref isFromPrefab);

            bool contentChanged = Voxel.ContentChanged;
            VRage.Serialization.MySerializer.Write(stream, ref contentChanged);

            if (contentChanged)
            {
                byte[] data;
                Voxel.Storage.Save(out data);
                VRage.Serialization.MySerializer.Write(stream, ref data);

                Console.WriteLine(String.Format("sending content : {0}", data.Length));
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

        protected override void OnLoad(BitStream stream, Action<MyVoxelBase> loadingDoneHandler)
        {
            MyVoxelBase map = null;

            bool isFromPrefab = VRage.Serialization.MySerializer.CreateAndRead<bool>(stream);
            bool rangeChanged = VRage.Serialization.MySerializer.CreateAndRead<bool>(stream);

            byte[] data = null;
            if (rangeChanged)
            {
                data = VRage.Serialization.MySerializer.CreateAndRead<byte[]>(stream);
            }

            if (isFromPrefab)
            {
                var builder = VRage.Serialization.MySerializer.CreateAndRead<MyObjectBuilder_EntityBase>(stream, MyObjectBuilderSerializer.Dynamic);
                var voxelMap = new MyVoxelMap();

                if (rangeChanged && data != null)
                {
                    IMyStorage storage = MyStorageBase.Load(data);
                    MyEntity entity;
                    if (MyEntities.TryGetEntityById(builder.EntityId, out entity) && entity is MyVoxelMap)
                    {
                        voxelMap = (entity as MyVoxelMap);
                        voxelMap.Storage = storage;
                    }
                    else
                    {
                        voxelMap.Init(builder, storage);
                        MyEntities.Add(voxelMap);
                    }
                   
                }
                else
                {
                    voxelMap.Init(builder);
                    MyEntities.Add(voxelMap);
                }
                map = voxelMap;
            }
            else
            {
                long voxelMapId = VRage.Serialization.MySerializer.CreateAndRead<long>(stream);

                MyEntity entity = null;
                MyEntities.TryGetEntityById(voxelMapId, out entity);

                map = entity as MyVoxelBase;
            }
            loadingDoneHandler(map);
        }

        public override void OnDestroy()
        {
            if (Voxel.Save)
            {
                Voxel.Close();
            }
        }

        public override void GetStateGroups(List<IMyStateGroup> resultList)
        {
            // No physics for voxels
        }
        #endregion
    }
}
