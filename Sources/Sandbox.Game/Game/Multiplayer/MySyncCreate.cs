using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Serializer;
using Sandbox.Common.ObjectBuilders.VRageData;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    static class MySyncCreate
    {
        [ProtoContract]
        [MessageId(37, P2PMessageEnum.Reliable)]
        struct CreateMsg
        {
            [ProtoMember]
            public MyObjectBuilder_EntityBase ObjectBuilder;
        }

        [ProtoContract]
        [MessageId(38, P2PMessageEnum.Reliable)]
        struct CreateCompressedMsg
        {
            [ProtoMember]
            public byte[] ObjectBuilders;

            [ProtoMember]
            public int[] BuilderLengths;
        }

        [ProtoContract]
        [MessageId(11873, P2PMessageEnum.Reliable)]
        struct MergingCopyPasteCompressedMsg
        {
            [ProtoMember]
            public CreateCompressedMsg CreateMessage;

            [ProtoMember]
            public long MergeGridId;

            [ProtoMember]
            public SerializableVector3I MergeOffset;

            [ProtoMember]
            public Base6Directions.Direction MergeForward;

            [ProtoMember]
            public Base6Directions.Direction MergeUp;
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

        [ProtoContract]
        [MessageId(11875, P2PMessageEnum.Reliable)]
        struct SpawnGridMsg
        {
            [ProtoMember]
            public MyObjectBuilder_CubeGrid Grid;
            
            [ProtoMember]
            public DefinitionIdBlit Definition;
            
            [ProtoMember]
            public Vector3D Position;
            
            [ProtoMember]
            public Vector3 Forward;
            
            [ProtoMember]
            public Vector3 Up;
            
            [ProtoMember]
            public bool Static;
        }

        static MySyncCreate()
        {
            MySyncLayer.RegisterMessage<CreateMsg>(OnMessage, MyMessagePermissions.Any, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<CreateCompressedMsg>(OnMessageCompressed, MyMessagePermissions.Any, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<MergingCopyPasteCompressedMsg>(OnMessageCompressedRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<CreateRelativeCompressedMsg>(OnMessageRelativeCompressed, MyMessagePermissions.Any, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<SpawnGridMsg>(OnMessageSpawnGrid, MyMessagePermissions.Any, MyTransportMessageEnum.Request);
        }

        static void OnMessage(ref CreateMsg msg, MyNetworkClient sender)
        {
            MySandboxGame.Log.WriteLine("CreateMsg: " + msg.ObjectBuilder.GetType().Name.ToString() + " EntityID: " + msg.ObjectBuilder.EntityId.ToString("X8"));
            MyEntities.CreateFromObjectBuilderAndAdd(msg.ObjectBuilder);
            MySandboxGame.Log.WriteLine("Status: Exists(" + MyEntities.EntityExists(msg.ObjectBuilder.EntityId) + ") InScene(" + ((msg.ObjectBuilder.PersistentFlags & MyPersistentEntityFlags2.InScene) == MyPersistentEntityFlags2.InScene) + ")");
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

            int bytesOffset = 0;
            for (int i = 0; i < msg.BuilderLengths.Length; ++i)
            {
                MemoryStream stream = new MemoryStream(msg.ObjectBuilders, bytesOffset, msg.BuilderLengths[i]);

                MyObjectBuilder_EntityBase entity;
                if (Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.DeserializeGZippedXML(stream, out entity))
                {
                    Debug.Assert(entity != null);
                    if (entity != null)
                    {
                        MySandboxGame.Log.WriteLine("CreateCompressedMsg: " + msg.ObjectBuilders.GetType().Name.ToString() + " EntityID: " + entity.EntityId.ToString("X8"));
                        MyEntities.CreateFromObjectBuilderAndAdd(entity);
                        MySandboxGame.Log.WriteLine("Status: Exists(" + MyEntities.EntityExists(entity.EntityId) + ") InScene(" + ((entity.PersistentFlags & MyPersistentEntityFlags2.InScene) == MyPersistentEntityFlags2.InScene) + ")");
                    }
                }

                bytesOffset += msg.BuilderLengths[i];
            }
        }

        static void OnMessageRelativeCompressed(ref CreateRelativeCompressedMsg msg, MyNetworkClient sender)
        {
            MySandboxGame.Log.WriteLine("CreateRelativeCompressedMsg received");

            int bytesOffset = 0;
            for (int i = 0; i < msg.CreateMessage.BuilderLengths.Length; ++i)
            {
                MemoryStream stream = new MemoryStream(msg.CreateMessage.ObjectBuilders, bytesOffset, msg.CreateMessage.BuilderLengths[i]);

                MyObjectBuilder_EntityBase entity;
                if (Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.DeserializeGZippedXML(stream, out entity))
                {
                    MySandboxGame.Log.WriteLine("CreateRelativeCompressedMsg: " + msg.CreateMessage.ObjectBuilders.GetType().Name.ToString() + " EntityID: " + entity.EntityId.ToString("X8"));

                    MyEntity baseEntity;
                    if (MyEntities.TryGetEntityById(msg.BaseEntity, out baseEntity))
                    {
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
        }

        public static void SendEntitiesCreated(List<MyObjectBuilder_EntityBase> entities)
        {
            CreateCompressedMsg msg;
            if (!BuildCompressedMessage(entities, out msg))
                return;

            MySession.Static.SyncLayer.SendMessageToAll(ref msg);
        }

        public static void SendEntityCreated(MyObjectBuilder_EntityBase entity)
        {
            var msg = new CreateCompressedMsg();

            MemoryStream stream = new MemoryStream();
            Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.SerializeXML(stream, (MyObjectBuilder_Base)entity, Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.XmlCompression.Gzip, typeof(MyObjectBuilder_EntityBase));

            Debug.Assert(stream.Length <= int.MaxValue);
            if (stream.Length > int.MaxValue)
            {
                MySandboxGame.Log.WriteLine("Cannot synchronize created entity: number of bytes when serialized is larger than int.MaxValue!");
                return;
            }
             
            msg.ObjectBuilders = stream.ToArray();
            msg.BuilderLengths = new int[1];
            msg.BuilderLengths[0] = (int)stream.Length;
            
            MySession.Static.SyncLayer.SendMessageToAll(ref msg);
        }

        public static void SendEntityCreatedRelative(MyObjectBuilder_EntityBase entity, MyEntity baseEntity, Vector3 relativeVelocity)
        {
            var msg = new CreateRelativeCompressedMsg();

            MemoryStream stream = new MemoryStream();
            Matrix relativeMatrix = entity.PositionAndOrientation.Value.GetMatrix() * baseEntity.PositionComp.WorldMatrixNormalizedInv;
            entity.PositionAndOrientation = new MyPositionAndOrientation(relativeMatrix);
            Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.SerializeXML(stream, (MyObjectBuilder_Base)entity, Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.XmlCompression.Gzip, typeof(MyObjectBuilder_EntityBase));

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

            MySession.Static.SyncLayer.SendMessageToAll(ref msg);
        }

        private static bool BuildCompressedMessage(List<MyObjectBuilder_EntityBase> entities, out CreateCompressedMsg msg)
        {
            msg = new CreateCompressedMsg();

            MemoryStream stream = new MemoryStream();
            List<int> lengths = new List<int>(8);

            long byteOffset = 0;
            foreach (var entity in entities)
            {
                Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.SerializeXML(stream, (MyObjectBuilder_Base)entity, Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.XmlCompression.Gzip, typeof(MyObjectBuilder_EntityBase));

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

        public static void RequestMergingCopyPaste(List<MyObjectBuilder_EntityBase> grids, long mergingGridId, MatrixI mergingTransform)
        {
            if (Sync.IsServer)
            {
                MySyncCreate.SendEntitiesCreated(grids);

                MyEntity entity;
                MyEntities.TryGetEntityById(mergingGridId, out entity);

                MyCubeGrid grid = entity as MyCubeGrid;
                Debug.Assert(grid != null);
                if (grid == null) return;

                MyEntity entity2;
                MyEntities.TryGetEntityById(grids[0].EntityId, out entity2);

                MyCubeGrid mergingGrid = entity2 as MyCubeGrid;
                Debug.Assert(mergingGrid != null);
                if (mergingGrid == null) return;

                grid.MergeGrid_CopyPaste(mergingGrid, mergingTransform);
            }
            else
            {
                MySyncCreate.SendMergingCopyPasteRequest(grids, mergingGridId, mergingTransform);
            }
        }

        private static void SendMergingCopyPasteRequest(List<MyObjectBuilder_EntityBase> grids, long mergingGridId, MatrixI mergingTransform)
        {
            MergingCopyPasteCompressedMsg msg;
            if (!BuildCompressedMessage(grids, out msg.CreateMessage))
                return;

            msg.MergeGridId = mergingGridId;
            msg.MergeOffset = mergingTransform.Translation;
            msg.MergeForward = mergingTransform.Forward;
            msg.MergeUp = mergingTransform.Up;

            MySession.Static.SyncLayer.SendMessageToServer(ref msg);
        }

        private static void OnMessageCompressedRequest(ref MergingCopyPasteCompressedMsg msg, MyNetworkClient sender)
        {
            MySandboxGame.Log.WriteLine("MergingCopyPasteCompressedMsg received");
            MySession.Static.SyncLayer.SendMessageToAllButOne(ref msg.CreateMessage, sender.SteamUserId);

            MyEntity firstEntity = OnMessageCompressedInternal(ref msg.CreateMessage);

            MyEntity entity;
            MyEntities.TryGetEntityById(msg.MergeGridId, out entity);

            MyCubeGrid grid = entity as MyCubeGrid;
            Debug.Assert(grid != null);
            if (grid == null) return;

            MyCubeGrid mergingGrid = firstEntity as MyCubeGrid;
            Debug.Assert(mergingGrid != null);
            if (mergingGrid == null) return;

            Vector3I offset = msg.MergeOffset;
            MatrixI mergeOffset = new MatrixI(ref offset, msg.MergeForward, msg.MergeUp);

            grid.MergeGrid_CopyPaste(mergingGrid, mergeOffset);
        }

        private static MyEntity OnMessageCompressedInternal(ref CreateCompressedMsg msg)
        {
            MyEntity firstEntity = null;

            int bytesOffset = 0;
            for (int i = 0; i < msg.BuilderLengths.Length; ++i)
            {
                MemoryStream stream = new MemoryStream(msg.ObjectBuilders, bytesOffset, msg.BuilderLengths[i]);

                MyObjectBuilder_EntityBase entity;
                if (Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.DeserializeGZippedXML(stream, out entity))
                {
                    MySandboxGame.Log.WriteLine("CreateCompressedMsg: " + msg.ObjectBuilders.GetType().Name.ToString() + " EntityID: " + entity.EntityId.ToString("X8"));
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


        public static void RequestStaticGridSpawn(MyCubeBlockDefinition definition, MatrixD worldMatrix)
        {
            SpawnGridMsg msg = new SpawnGridMsg();
            
            msg.Definition = definition.Id;
            msg.Position = worldMatrix.Translation;
            msg.Forward = worldMatrix.Forward;
            msg.Up = worldMatrix.Up;
            msg.Static = true;

            MySession.Static.SyncLayer.SendMessageToServer(ref msg);
        }

        public static void RequestDynamicGridSpawn(MyCubeBlockDefinition definition, MatrixD worldMatrix)
        {
            SpawnGridMsg msg = new SpawnGridMsg();

            msg.Definition = definition.Id;
            msg.Position = worldMatrix.Translation;
            msg.Forward = worldMatrix.Forward;
            msg.Up = worldMatrix.Up;
            msg.Static = false;

            MySession.Static.SyncLayer.SendMessageToServer(ref msg);
        }

        static void OnMessageSpawnGrid(ref SpawnGridMsg msg, MyNetworkClient sender)
        {
            if (Sync.IsServer)
            {
                var definition = Definitions.MyDefinitionManager.Static.GetCubeBlockDefinition(msg.Definition);
                MatrixD worldMatrix = MatrixD.CreateWorld(msg.Position, msg.Forward, msg.Up);

                MyCubeGrid grid = null;

                if (msg.Static)
                    grid = MyCubeBuilder.SpawnStaticGrid(definition, worldMatrix);
                else
                    grid = MyCubeBuilder.SpawnDynamicGrid(definition, worldMatrix);

                if (grid != null)
                {
                    msg.Grid = grid.GetObjectBuilder() as MyObjectBuilder_CubeGrid;
                    MySession.Static.SyncLayer.SendMessageToAll(ref msg);
                }

                if (grid != null && msg.Static)
                {
                    MyCubeBuilder.AfterStaticGridSpawn(grid);
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(msg.Grid != null, "Client must obtain complete grid from server");

                MyCubeGrid grid = MyEntities.CreateFromObjectBuilderAndAdd(msg.Grid) as MyCubeGrid;

                if (grid != null)
                {
                    if (grid.IsStatic)
                        MyCubeBuilder.AfterStaticGridSpawn(grid);
                }
            }
        }
    }
}
