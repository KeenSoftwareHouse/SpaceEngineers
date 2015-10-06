using Havok;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.VRageData;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GUI;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using VRage;
using VRage.Components;
using VRage.ModAPI;
using VRage.Network;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    public static class MySyncCreate
    {
        [ProtoContract]
        [MessageId(37, P2PMessageEnum.Reliable)]
        struct CreateMsg
        {
            [ProtoMember]
            public MyObjectBuilder_EntityBase ObjectBuilder;
        }

        [ProtoContract]
        [MessageId(32, P2PMessageEnum.Reliable)]
        struct CreateCompressedMsg
        {
            [ProtoMember]
            public int PlayerSerialId;
            
            [ProtoMember]
            public byte[] ObjectBuilders;

            [ProtoMember]
            public int[] BuilderLengths;
        }

        [ProtoContract]
        [MessageId(11874, P2PMessageEnum.Reliable)]
        struct CreateRelativeCompressedMsg
        {
            [ProtoMember]
            public CreateCompressedMsg CreateMessage;

            [ProtoMember]
            public long BaseEntity;

            [ProtoMember]
            public SerializableVector3 RelativeVelocity;
        }

        [MessageId(11875, P2PMessageEnum.Reliable)]
        struct SpawnGridMsg
        {
            public long BuilderEntityId;
            public DefinitionIdBlit Definition;
            public Vector3D Position;
            public Vector3 Forward;
            public Vector3 Up;
            public BoolBlit Static;
        }

        [MessageId(11876, P2PMessageEnum.Reliable)]
        struct SpawnGridReplyMsg
        { }

        [MessageId(11877, P2PMessageEnum.Reliable)]
        struct AfterGridCreatedMsg
        {
            public long BuilderEntityId;
            public long GridEntityId;
        }

        [ProtoContract]
        [MessageId(11879, P2PMessageEnum.Reliable)]
        struct CreateAndInitMsg
        {
            [ProtoMember]
            public byte[] ObjectBuilder;

            [ProtoMember]
            public int BuilderLength;

            [ProtoMember]
            public SerializableDefinitionId DefinitionId;
        }

        static MySyncCreate()
        {
            MySyncLayer.RegisterMessage<CreateMsg>(OnMessage, MyMessagePermissions.FromServer | MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<CreateCompressedMsg>(OnMessageCompressed, MyMessagePermissions.FromServer|MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<CreateRelativeCompressedMsg>(OnMessageRelativeCompressed, MyMessagePermissions.FromServer|MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<SpawnGridMsg>(OnMessageSpawnGrid, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<SpawnGridReplyMsg>(OnMessageSpawnGridSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<SpawnGridReplyMsg>(OnMessageSpawnGridFailure, MyMessagePermissions.FromServer, MyTransportMessageEnum.Failure);
            MySyncLayer.RegisterMessage<AfterGridCreatedMsg>(OnMessageAfterGridCreated, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<CreateAndInitMsg>(OnMessageCreateAndInit, MyMessagePermissions.FromServer);
        }
        
        public static void RequestEntityCreate(MyObjectBuilder_EntityBase entityBuilder)
        {
            Debug.Fail("Use replication instead of this!");
            var msg = new CreateMsg() { ObjectBuilder = entityBuilder, };
            MySession.Static.SyncLayer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void OnMessage(ref CreateMsg msg, MyNetworkClient sender)
        {
            Debug.Fail("Use replication instead of this!");
            MySandboxGame.Log.WriteLine("CreateMsg: Type: " + msg.ObjectBuilder.GetType().Name.ToString() + "  Name: " + msg.ObjectBuilder.Name + "  EntityID: " + msg.ObjectBuilder.EntityId.ToString("X8"));
            MyEntities.CreateFromObjectBuilderAndAdd(msg.ObjectBuilder);
            MySandboxGame.Log.WriteLine("Status: Exists(" + MyEntities.EntityExists(msg.ObjectBuilder.EntityId) + ") InScene(" + ((msg.ObjectBuilder.PersistentFlags & MyPersistentEntityFlags2.InScene) == MyPersistentEntityFlags2.InScene) + ")");
            if (Sync.IsServer)
                MySession.Static.SyncLayer.SendMessageToAll(ref msg);
        }

        static void OnMessageCompressed(ref CreateCompressedMsg msg, MyNetworkClient sender)
        {
            MySandboxGame.Log.WriteLine("CreateCompressedMsg received");

            Debug.Assert(msg.BuilderLengths != null);
            Debug.Assert(msg.ObjectBuilders != null);

            if (msg.BuilderLengths == null)
                return;
            if (msg.ObjectBuilders == null)
                return;

            OnMessageCompressedInternal(ref msg);
          
        }

        static void OnMessageRelativeCompressed(ref CreateRelativeCompressedMsg msg, MyNetworkClient sender)
        {
            Debug.Fail("Use replication instead of this!");
            MySandboxGame.Log.WriteLine("CreateRelativeCompressedMsg received");

            int bytesOffset = 0;
            for (int i = 0; i < msg.CreateMessage.BuilderLengths.Length; ++i)
            {
                MemoryStream stream = new MemoryStream(msg.CreateMessage.ObjectBuilders, bytesOffset, msg.CreateMessage.BuilderLengths[i]);

                MyObjectBuilder_EntityBase entity;
                if (MyObjectBuilderSerializer.DeserializeGZippedXML(stream, out entity))
                {
                    MyEntity baseEntity;
                    if (MyEntities.TryGetEntityById(msg.BaseEntity, out baseEntity))
                    {
                        MySandboxGame.Log.WriteLine("CreateRelativeCompressedMsg: Type: " + baseEntity.GetType().Name.ToString() + "  Name: " + baseEntity.Name + "  EntityID: " + baseEntity.EntityId.ToString("X8"));

                        Matrix worldMatrix = entity.PositionAndOrientation.Value.GetMatrix() * baseEntity.WorldMatrix;
                        entity.PositionAndOrientation = new MyPositionAndOrientation(worldMatrix);

                        var newEntity = MyEntities.CreateFromObjectBuilderAndAdd(entity);

                        Vector3 velocity = Vector3.Transform(msg.RelativeVelocity, baseEntity.WorldMatrix.GetOrientation());
                        newEntity.Physics.LinearVelocity = velocity;

                        MySandboxGame.Log.WriteLine("Status: Exists(" + MyEntities.EntityExists(entity.EntityId) + ") InScene(" + ((entity.PersistentFlags & MyPersistentEntityFlags2.InScene) == MyPersistentEntityFlags2.InScene) + ")");
                    }
                }

                bytesOffset += msg.CreateMessage.BuilderLengths[i];
            }

            if (Sync.IsServer)
            {
                MySession.Static.SyncLayer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
            }
        }

        public static void SendEntitiesCreated(List<MyObjectBuilder_CubeGrid> entities, bool detectDisconnects, long inventoryEntityId)
        {
            CreateCompressedMsg msg;
            if (!BuildCompressedMessage(entities, out msg))
                return;

            MySession.Static.SyncLayer.SendMessageToServer(ref msg);
        }

        public static void SendEntityCreated(MyObjectBuilder_EntityBase entity)
        {
            Debug.Fail("Use replication instead of this!");
            var msg = new CreateCompressedMsg();

            MemoryStream stream = new MemoryStream();
            MyObjectBuilderSerializer.SerializeXML(stream, (MyObjectBuilder_Base)entity, MyObjectBuilderSerializer.XmlCompression.Gzip, typeof(MyObjectBuilder_EntityBase));

            Debug.Assert(stream.Length <= int.MaxValue);
            if (stream.Length > int.MaxValue)
            {
                MySandboxGame.Log.WriteLine("Cannot synchronize created entity: number of bytes when serialized is larger than int.MaxValue!");
                return;
            }

            msg.ObjectBuilders = stream.ToArray();
            msg.BuilderLengths = new int[1];
            msg.BuilderLengths[0] = (int)stream.Length;

            if (Sync.IsServer)
            {
                MySession.Static.SyncLayer.SendMessageToAll(ref msg);
            }
            else
            {
                MySession.Static.SyncLayer.SendMessageToServer(ref msg);
            }
        }

        public static void SendEntityCreatedRelative(MyObjectBuilder_EntityBase entity, MyEntity baseEntity, Vector3 relativeVelocity)
        {
            Debug.Fail("Use replication instead of this!");
            var msg = new CreateRelativeCompressedMsg();

            MemoryStream stream = new MemoryStream();
            Matrix relativeMatrix = entity.PositionAndOrientation.Value.GetMatrix() * baseEntity.PositionComp.WorldMatrixNormalizedInv;
            entity.PositionAndOrientation = new MyPositionAndOrientation(relativeMatrix);
            MyObjectBuilderSerializer.SerializeXML(stream, (MyObjectBuilder_Base)entity, MyObjectBuilderSerializer.XmlCompression.Gzip, typeof(MyObjectBuilder_EntityBase));

            Debug.Assert(stream.Length <= int.MaxValue);
            if (stream.Length > int.MaxValue)
            {
                MySandboxGame.Log.WriteLine("Cannot synchronize created entity: number of bytes when serialized is larger than int.MaxValue!");
                return;
            }

            msg.CreateMessage.ObjectBuilders = stream.ToArray();
            msg.CreateMessage.BuilderLengths = new int[1];
            msg.CreateMessage.BuilderLengths[0] = (int)stream.Length;

            msg.BaseEntity = baseEntity.EntityId;
            msg.RelativeVelocity = relativeVelocity;

            if (Sync.IsServer)
            {
                MySession.Static.SyncLayer.SendMessageToAll(ref msg);
            }
            else
            {
                MySession.Static.SyncLayer.SendMessageToServer(ref msg);
            }
        }

        private static bool BuildCompressedMessage(List<MyObjectBuilder_CubeGrid> entities, out CreateCompressedMsg msg)
        {
            msg = new CreateCompressedMsg();

            MemoryStream stream = new MemoryStream();
            List<int> lengths = new List<int>(8);

            long byteOffset = 0;
            foreach (var entity in entities)
            {
                MyObjectBuilderSerializer.SerializeXML(stream, (MyObjectBuilder_Base)entity, MyObjectBuilderSerializer.XmlCompression.Gzip, typeof(MyObjectBuilder_EntityBase));

                Debug.Assert(stream.Length <= int.MaxValue);
                if (stream.Length > int.MaxValue)
                {
                    MySandboxGame.Log.WriteLine("Cannot synchronize created entity: number of bytes when serialized is larger than int.MaxValue!");
                    return false;
                }

                lengths.Add((int)(stream.Length - byteOffset));
                byteOffset = stream.Length;
            }

            msg.ObjectBuilders = stream.ToArray();
            msg.BuilderLengths = lengths.ToArray();
            return true;
        }

        private static MyEntity OnMessageCompressedInternal(ref CreateCompressedMsg msg)
        {
            Debug.Fail("Use replication instead of this!");
            MyEntity firstEntity = null;

            int bytesOffset = 0;
            for (int i = 0; i < msg.BuilderLengths.Length; ++i)
            {
                MemoryStream stream = new MemoryStream(msg.ObjectBuilders, bytesOffset, msg.BuilderLengths[i]);

                MyObjectBuilder_EntityBase entity;
                if (MyObjectBuilderSerializer.DeserializeGZippedXML(stream, out entity))
                {
                    MySandboxGame.Log.WriteLine("CreateCompressedMsg: Type: " + entity.GetType().Name.ToString() + "  Name: " + entity.Name + "  EntityID: " + entity.EntityId.ToString("X8"));
                    if (i == 0)
                        firstEntity = MyEntities.CreateFromObjectBuilderAndAdd(entity);
                    else
                        MyEntities.CreateFromObjectBuilderAndAdd(entity);
                    MySandboxGame.Log.WriteLine("Status: Exists(" + MyEntities.EntityExists(entity.EntityId) + ") InScene(" + ((entity.PersistentFlags & MyPersistentEntityFlags2.InScene) == MyPersistentEntityFlags2.InScene) + ")");
                }

                bytesOffset += msg.BuilderLengths[i];
            }

            return firstEntity;
        }

        public static void RequestStaticGridSpawn(MyCubeBlockDefinition definition, MatrixD worldMatrix, long builderEntityId)
        {
            Debug.Fail("Use replication instead of this!");
            SpawnGridMsg msg = new SpawnGridMsg();

            msg.Definition = definition.Id;
            msg.Position = worldMatrix.Translation;
            msg.Forward = worldMatrix.Forward;
            msg.Up = worldMatrix.Up;
            msg.Static = true;
            msg.BuilderEntityId = builderEntityId;

            MySession.Static.SyncLayer.SendMessageToServer(ref msg);
        }

        public static void RequestDynamicGridSpawn(MyCubeBlockDefinition definition, MatrixD worldMatrix, long builderEntityId)
        {
            Debug.Fail("Use replication instead of this!");
            SpawnGridMsg msg = new SpawnGridMsg();

            msg.Definition = definition.Id;
            msg.Position = worldMatrix.Translation;
            msg.Forward = worldMatrix.Forward;
            msg.Up = worldMatrix.Up;
            msg.Static = false;
            msg.BuilderEntityId = builderEntityId;

            MySession.Static.SyncLayer.SendMessageToServer(ref msg);
        }

        static void OnMessageSpawnGrid(ref SpawnGridMsg msg, MyNetworkClient sender)
        {
            Debug.Fail("Use replication instead of this!");
            Debug.Assert(MyCubeBuilder.BuildComponent != null, "The build component was not set in cube builder!");

            MyEntity builder = null;
            MyEntities.TryGetEntityById(msg.BuilderEntityId, out builder);

            var definition = Definitions.MyDefinitionManager.Static.GetCubeBlockDefinition(msg.Definition);
            MatrixD worldMatrix = MatrixD.CreateWorld(msg.Position, msg.Forward, msg.Up);

            var reply = new SpawnGridReplyMsg();

            MyCubeBuilder.BuildComponent.GetGridSpawnMaterials(definition, worldMatrix, msg.Static);
            bool canSpawn = MyCubeBuilder.BuildComponent.HasBuildingMaterials(builder);

            MySession.Static.SyncLayer.SendMessage(ref reply, sender.SteamUserId, canSpawn ? MyTransportMessageEnum.Success : MyTransportMessageEnum.Failure);

            if (!canSpawn) return;

            MyCubeBuilder.SpawnGrid(definition, worldMatrix, builder, msg.Static);
        }

        static void OnMessageSpawnGridFailure(ref SpawnGridReplyMsg msg, MyNetworkClient sender)
        {
            MyGuiAudio.PlaySound(MyGuiSounds.HudUnable);
        }

        static void OnMessageSpawnGridSuccess(ref SpawnGridReplyMsg msg, MyNetworkClient sender)
        {
            MyGuiAudio.PlaySound(MyGuiSounds.HudPlaceBlock);
        }

        public static void SendAfterGridBuilt(long builderId, long gridId)
        {
            Debug.Fail("Use replication instead of this!");
            var msg = new AfterGridCreatedMsg();
            msg.BuilderEntityId = builderId;
            msg.GridEntityId = gridId;

            MySession.Static.SyncLayer.SendMessageToAll(ref msg);
        }

        static void OnMessageAfterGridCreated(ref AfterGridCreatedMsg msg, MyNetworkClient sender)
        {
            Debug.Fail("Use replication instead of this!");
            MyEntity builder;
            MyEntity gridEntity;
            MyCubeGrid grid;
            MyEntities.TryGetEntityById(msg.BuilderEntityId, out builder);
            MyEntities.TryGetEntityById(msg.GridEntityId, out gridEntity);

            grid = gridEntity as MyCubeGrid;
            Debug.Assert(grid != null, "Could not find the grid entity!");
            if (grid == null)
            {
                return;
            }

            MyCubeBuilder.AfterGridBuild(builder, grid);
        }


        public static void SendEntityCreated(MyObjectBuilder_EntityBase entityBuilder, MyDefinitionId myDefinitionId)
        {
            Debug.Fail("Use replication instead of this!");
            var msg = new CreateAndInitMsg();

            MemoryStream stream = new MemoryStream();
            MyObjectBuilderSerializer.SerializeXML(stream, (MyObjectBuilder_Base)entityBuilder, MyObjectBuilderSerializer.XmlCompression.Gzip, typeof(MyObjectBuilder_EntityBase));

            Debug.Assert(stream.Length <= int.MaxValue);
            if (stream.Length > int.MaxValue)
            {
                MySandboxGame.Log.WriteLine("Cannot synchronize created entity: number of bytes when serialized is larger than int.MaxValue!");
                return;
            }

            msg.ObjectBuilder = stream.ToArray();
            msg.BuilderLength = (int)stream.Length;
            msg.DefinitionId = myDefinitionId;

            MySession.Static.SyncLayer.SendMessageToAll(ref msg);
        }


        static void OnMessageCreateAndInit(ref CreateAndInitMsg msg, MyNetworkClient sender)
        {
            Debug.Fail("Use replication instead of this!");
            MemoryStream stream = new MemoryStream(msg.ObjectBuilder, 0, msg.BuilderLength);

            MyObjectBuilder_EntityBase entityBuilder;
            if (MyObjectBuilderSerializer.DeserializeGZippedXML(stream, out entityBuilder))
            {
                if (entityBuilder == null)
                {
                    Debug.Fail("Object builder was not deserialized");
                    return;
                }
            }
            else
            {
                Debug.Fail("Deserialization failed");
                return;
            }

            MyEntities.CreateAndAddFromDefinition(entityBuilder, msg.DefinitionId);
        }
    }
}
