#region Using

using System;
using System.Collections.Generic;
using Sandbox.Engine.Multiplayer;
using VRageMath;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities;
using VRage.Serialization;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using System.Runtime.InteropServices;
using SteamSDK;
using VRageMath.PackedVector;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using VRage;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using VRage.ObjectBuilders;
using Sandbox.Engine.Physics;
using VRage.ModAPI;
using System.Diagnostics;
using Sandbox.Game.Entities.Character;

#endregion

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    public class MySyncFloatingObjects
    {
        static MyFloatingObjects m_floatingObjects;
        static System.Collections.Generic.List<Havok.HkBodyCollision> m_rigidBodyList = new System.Collections.Generic.List<Havok.HkBodyCollision>();

        struct FloatingObjectsData
        {
            public DefinitionIdBlit TypeId;
            public List<MyFloatingObject> Instances;
        }
        Dictionary<int, FloatingObjectsData> m_sortingMap = new Dictionary<int, FloatingObjectsData>();
        static List<MakeStableEntityData> m_tmpStableData = new List<MakeStableEntityData>();
        static List<long> m_tmpNonStableIds = new List<long>();

        [MessageId(1630, P2PMessageEnum.Unreliable)]
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct FloatingObjectPositionUpdateMsg : IEntityMessage
        {
            // 68 B total
            public long EntityId;

            public Vector3D Position;
            public Vector3 Forward;
            public Vector3 Up;

            public Vector3 LinearVelocity;
            public Vector3 AngularVelocity;

            public long GetEntityId() { return EntityId; }

            public override string ToString()
            {
                return String.Format("{0}, {1}, Velocity: {2}", this.GetType().Name, this.GetEntityText(), LinearVelocity.ToString());
            }
        }
        public static int PositionUpdateMsgSize = BlitSerializer<FloatingObjectPositionUpdateMsg>.StructSize;

        public const int MAX_SYNC_ON_REQUEST = 5;
        public static List<MyFloatingObject> tmp_list = new List<MyFloatingObject>(MAX_SYNC_ON_REQUEST);

        [MessageId(10150, P2PMessageEnum.Reliable)]
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        protected struct PositionUpdateCompressedMsg : IEntityMessage
        {
            public long EntityId;

            Vector3D Position;
            HalfVector4 Orientation;

            HalfVector2 LinearVelocityXY;
            HalfVector2 LinearVelocityZAndAngularVelocityX;
            HalfVector2 AngularVelocityYZ;

            public long GetEntityId() { return EntityId; }

            public void SetWorldMatrix(MatrixD worldMatrix)
            {
                Position = worldMatrix.Translation;
                Quaternion quat = Quaternion.CreateFromRotationMatrix(worldMatrix);
                Orientation = new HalfVector4(quat.X, quat.Y, quat.Z, quat.W);
            }

            public void SetVelocities(Vector3 linearVelocity, Vector3 angularVelocity)
            {
                LinearVelocityXY = new HalfVector2(linearVelocity.X, linearVelocity.Y);
                LinearVelocityZAndAngularVelocityX = new HalfVector2(linearVelocity.Z, angularVelocity.X);
                AngularVelocityYZ = new HalfVector2(angularVelocity.Y, angularVelocity.Z);
            }

            public MatrixD GetWorldMatrix()
            {
                Vector4 oriV4 = Orientation.ToVector4();
                Quaternion quat = new Quaternion(oriV4.X, oriV4.Y, oriV4.Z, oriV4.W);
                quat.Normalize();
                Matrix m = Matrix.CreateFromQuaternion(quat);
                m.Translation = Position;

                return m;
            }

            public void GetVelocities(out Vector3 linearVelocity, out Vector3 angularVelocity)
            {
                Vector2 linXY = LinearVelocityXY.ToVector2();
                Vector2 linZAngX = LinearVelocityZAndAngularVelocityX.ToVector2();
                Vector2 angYZ = AngularVelocityYZ.ToVector2();

                linearVelocity = new Vector3(linXY.X, linXY.Y, linZAngX.X);
                angularVelocity = new Vector3(linZAngX.Y, angYZ.X, angYZ.Y);
            }

        }

        [MessageId(10151, P2PMessageEnum.Reliable)]
        public struct RemoveFloatingObjectMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public MyFixedPoint Amount;
        }

        [MessageId(10152, P2PMessageEnum.Reliable)]
        public struct AddFloatingObjectMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public MyFixedPoint Amount;
        }

        [MessageId(10155, P2PMessageEnum.Unreliable)]
        struct MakeStableBatchMsg
        {
            public List<MakeStableEntityData> StableObjects;
        }

        public struct MakeStableEntityData
        {
            public long EntityId;
            public Vector3D Position;
            public Vector3D Forward;
            public Vector3D Up;

            public MyPositionAndOrientation GetPositionAndOrientation()
            {
                return new MyPositionAndOrientation(Position, Forward, Up);
            }
        }

        struct RelativeEntityData
        {
            public long EntityId;
            public HalfVector3 RelPosition;
            public HalfVector3 RelForward;
            public HalfVector3 RelUp;
        }

        [MessageId(10156, P2PMessageEnum.Unreliable)]
        struct MakeUnstableBatchMsg
        {
            public List<long> Entities;
        }

        [MessageId(10157, P2PMessageEnum.Unreliable)]
        struct MakeStableBatchReqMsg
        {
            public List<long> Entities;
        }

        struct CreatedFloatingObject
        {
            public DefinitionIdBlit TypeId; // 6B
            public List<CreatedFloatingObjectInstance> Instances;
        }

        // 84 B
        struct CreatedFloatingObjectInstance
        {
            public MyFixedPoint Amount; // 8 B
            public FloatingObjectPositionUpdateMsg Location; // 68 B
        }

        [MessageId(10153, P2PMessageEnum.Reliable)]
        struct FloatingObjectsCreateMsg
        {
            public List<CreatedFloatingObject> FloatingObjects;
        }

        [MessageId(10154, P2PMessageEnum.Reliable)]
        struct FloatingObjectRequestPosMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }
        }

        class FloatingObjectsSerializer : ISerializer<FloatingObjectsCreateMsg>
        {

            void ISerializer<FloatingObjectsCreateMsg>.Serialize(ByteStream destination, ref FloatingObjectsCreateMsg data)
            {
                int count = data.FloatingObjects.Count;
                BlitSerializer<int>.Default.Serialize(destination, ref count);
                for (int i = 0; i < count; i++)
                {
                    var floatingObjectData = data.FloatingObjects[i];
                    BlitSerializer<DefinitionIdBlit>.Default.Serialize(destination, ref floatingObjectData.TypeId);

                    int instancesCount = floatingObjectData.Instances.Count;
                    BlitSerializer<int>.Default.Serialize(destination, ref instancesCount);

                    for (int j = 0; j < instancesCount; j++)
                    {
                        // 84 B per instance
                        var floatingObjectInstance = floatingObjectData.Instances[j];
                        BlitSerializer<MyFixedPoint>.Default.Serialize(destination, ref floatingObjectInstance.Amount);
                        BlitSerializer<long>.Default.Serialize(destination, ref floatingObjectInstance.Location.EntityId);
                        BlitSerializer<Vector3D>.Default.Serialize(destination, ref floatingObjectInstance.Location.Position);
                        BlitSerializer<Vector3>.Default.Serialize(destination, ref floatingObjectInstance.Location.Forward);
                        BlitSerializer<Vector3>.Default.Serialize(destination, ref floatingObjectInstance.Location.Up);
                        BlitSerializer<Vector3>.Default.Serialize(destination, ref floatingObjectInstance.Location.LinearVelocity);
                        BlitSerializer<Vector3>.Default.Serialize(destination, ref floatingObjectInstance.Location.AngularVelocity);
                    }
                }
            }

            void ISerializer<FloatingObjectsCreateMsg>.Deserialize(ByteStream source, out FloatingObjectsCreateMsg msg)
            {
                msg = new FloatingObjectsCreateMsg();
                msg.FloatingObjects = new List<CreatedFloatingObject>();

                int count;
                BlitSerializer<int>.Default.Deserialize(source, out count);
                for (int i = 0; i < count; i++)
                {
                    CreatedFloatingObject data = new CreatedFloatingObject();
                    data.Instances = new List<CreatedFloatingObjectInstance>();

                    BlitSerializer<DefinitionIdBlit>.Default.Deserialize(source, out data.TypeId);

                    int instancesCount;
                    BlitSerializer<int>.Default.Deserialize(source, out instancesCount);
                    for (int j = 0; j < instancesCount; j++)
                    {
                        CreatedFloatingObjectInstance instance = new CreatedFloatingObjectInstance();
                        instance.Location = new FloatingObjectPositionUpdateMsg();
                        BlitSerializer<MyFixedPoint>.Default.Deserialize(source, out instance.Amount);

                        BlitSerializer<long>.Default.Deserialize(source, out instance.Location.EntityId);
                        BlitSerializer<Vector3D>.Default.Deserialize(source, out instance.Location.Position);
                        BlitSerializer<Vector3>.Default.Deserialize(source, out instance.Location.Forward);
                        BlitSerializer<Vector3>.Default.Deserialize(source, out instance.Location.Up);
                        BlitSerializer<Vector3>.Default.Deserialize(source, out instance.Location.LinearVelocity);
                        BlitSerializer<Vector3>.Default.Deserialize(source, out instance.Location.AngularVelocity);

                        data.Instances.Add(instance);
                    }

                    msg.FloatingObjects.Add(data);
                }
            }
        }

        class MakeStableBatchReqSerializer : ISerializer<MakeStableBatchReqMsg>
        {
            void ISerializer<MakeStableBatchReqMsg>.Serialize(ByteStream destination, ref MakeStableBatchReqMsg data)
            {
                destination.Write7BitEncodedInt(data.Entities.Count);
                for (int i = 0; i < data.Entities.Count; ++i)
                {
                    var l = data.Entities[i];
                    BlitSerializer<long>.Default.Serialize(destination, ref l);
                }
            }

            void ISerializer<MakeStableBatchReqMsg>.Deserialize(ByteStream source, out MakeStableBatchReqMsg data)
            {
                data = new MakeStableBatchReqMsg();
                int length = source.Read7BitEncodedInt();
                data.Entities = new List<long>(length);
                for (int i = 0; i < length; ++i)
                {
                    long id;
                    BlitSerializer<long>.Default.Deserialize(source, out id);
                    data.Entities.Add(id);
                }
            }
        }

        class MakeStableBatchSerializer : ISerializer<MakeStableBatchMsg>
        {
            public void Serialize(ByteStream destination, ref MakeStableBatchMsg data)
            {
                var objs = data.StableObjects;
                var count = objs.Count;
                destination.Write7BitEncodedInt(count);
                if (count == 0)
                    return;
                var firstIns = objs[0];
                var firstPos = firstIns.Position;
                var firstForward = firstIns.Forward;
                var firstUp = firstIns.Up;
                BlitSerializer<long>.Default.Serialize(destination, ref firstIns.EntityId);
                BlitSerializer<Vector3D>.Default.Serialize(destination, ref firstPos);
                BlitSerializer<Vector3D>.Default.Serialize(destination, ref firstForward);
                BlitSerializer<Vector3D>.Default.Serialize(destination, ref firstUp);
                for (int i = 1; i < count; ++i)
                {
                    var ins = objs[i];
                    BlitSerializer<long>.Default.Serialize(destination, ref ins.EntityId);
                    HalfVector3 rel = (Vector3)(ins.Position - firstPos);
                    BlitSerializer<HalfVector3>.Default.Serialize(destination, ref rel);
                    rel = (Vector3)(ins.Forward - firstForward);
                    BlitSerializer<HalfVector3>.Default.Serialize(destination, ref rel);
                    rel = (Vector3)(ins.Up - firstUp);
                    BlitSerializer<HalfVector3>.Default.Serialize(destination, ref rel);
                }
            }

            public void Deserialize(ByteStream source, out MakeStableBatchMsg data)
            {
                data = new MakeStableBatchMsg();
                var count = source.Read7BitEncodedInt();
                var objs = new List<MakeStableEntityData>(count);
                if (count == 0)
                    return;
                var firstData = new MakeStableEntityData();
                firstData.EntityId = source.ReadInt64();
                BlitSerializer<Vector3D>.Default.Deserialize(source, out firstData.Position);
                BlitSerializer<Vector3D>.Default.Deserialize(source, out firstData.Forward);
                BlitSerializer<Vector3D>.Default.Deserialize(source, out firstData.Up);
                objs.Add(firstData);
                for (int i = 1; i < count; ++i)
                {
                    HalfVector3 tmp;
                    var newIns = new MakeStableEntityData();
                    BlitSerializer<long>.Default.Deserialize(source, out newIns.EntityId);
                    BlitSerializer<HalfVector3>.Default.Deserialize(source, out tmp);
                    newIns.Position = firstData.Position + tmp;
                    BlitSerializer<HalfVector3>.Default.Deserialize(source, out tmp);
                    newIns.Forward = firstData.Forward + tmp;
                    BlitSerializer<HalfVector3>.Default.Deserialize(source, out tmp);
                    newIns.Up = firstData.Up + tmp;
                    objs.Add(newIns);
                }
                data.StableObjects = objs;
            }
        }

        class MakeUnstableBatchSerializer : ISerializer<MakeUnstableBatchMsg>
        {
            void ISerializer<MakeUnstableBatchMsg>.Serialize(ByteStream destination, ref MakeUnstableBatchMsg data)
            {
                destination.Write7BitEncodedInt(data.Entities.Count);
                for (int i = 0; i < data.Entities.Count; ++i)
                {
                    var l = data.Entities[i];
                    BlitSerializer<long>.Default.Serialize(destination, ref l);
                }
            }

            void ISerializer<MakeUnstableBatchMsg>.Deserialize(ByteStream source, out MakeUnstableBatchMsg data)
            {
                data = new MakeUnstableBatchMsg();
                int length = source.Read7BitEncodedInt();
                data.Entities = new List<long>(length);
                for (int i = 0; i < length; ++i)
                {
                    long id;
                    BlitSerializer<long>.Default.Deserialize(source, out id);
                    data.Entities.Add(id);
                }
            }
        }

        public static int PositionUpdateCompressedMsgSize = BlitSerializer<PositionUpdateCompressedMsg>.StructSize;
        public static int MakeStableBatchEntityDataSize = BlitSerializer<MakeStableEntityData>.StructSize;
        public static int RelativeEntityDataSize = BlitSerializer<RelativeEntityData>.StructSize;

        static MySyncFloatingObjects()
        {
            MySyncLayer.RegisterMessage<FloatingObjectPositionUpdateMsg>(OnUpdateCallback, MyMessagePermissions.FromServer);
            MySyncLayer.RegisterMessage<PositionUpdateCompressedMsg>(OnUpdateCompressedCallback, MyMessagePermissions.FromServer);
            MySyncLayer.RegisterMessage<RemoveFloatingObjectMsg>(RemoveFloatingObjectRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<AddFloatingObjectMsg>(AddFloatingObjectSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<RemoveFloatingObjectMsg>(RemoveFloatingObjectSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<FloatingObjectsCreateMsg>(OnCreateFloatingObjectsCallback, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request, new FloatingObjectsSerializer());
            MySyncLayer.RegisterMessage<FloatingObjectRequestPosMsg>(OnFloatingObjectsRequestPosCallback, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);

            //MySyncLayer.RegisterMessage<MakeStableMsg>(OnMakeStableSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            //MySyncLayer.RegisterMessage<MakeStableReqMsg>(OnMakeStableRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            //MySyncLayer.RegisterMessage<MakeStableReqMsg>(OnMakeStableFailure, MyMessagePermissions.FromServer, MyTransportMessageEnum.Failure);
            //MySyncLayer.RegisterMessage<MakeUnstableMsg>(OnMakeUnstableSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<MakeStableBatchReqMsg>(OnMakeStableBatchReq, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request, new MakeStableBatchReqSerializer());
            MySyncLayer.RegisterMessage<MakeStableBatchReqMsg>(OnMakeStableBatchFailure, MyMessagePermissions.FromServer, MyTransportMessageEnum.Failure, new MakeStableBatchReqSerializer());
            MySyncLayer.RegisterMessage<MakeStableBatchMsg>(OnMakeStableBatchSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success, new MakeStableBatchSerializer());
            MySyncLayer.RegisterMessage<MakeUnstableBatchMsg>(OnMakeUnstableBatchSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success, new MakeUnstableBatchSerializer());
        }        

        public MySyncFloatingObjects(MyFloatingObjects floatingObjects)
        {
            m_floatingObjects = floatingObjects;
        }

        public void SendRemoveFloatingObjectRequest(MyFloatingObject floatingObject, MyFixedPoint amount)
        {
            System.Diagnostics.Debug.Assert(!Sync.IsServer);
            var msg = new RemoveFloatingObjectMsg();
            msg.EntityId = floatingObject.EntityId;
            msg.Amount = amount;
         
            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        public void SendAddFloatingObject(MyFloatingObject floatingObject, MyFixedPoint amount)
        {
            System.Diagnostics.Debug.Assert(Sync.IsServer);
            var msg = new AddFloatingObjectMsg();
            msg.EntityId = floatingObject.EntityId;
            msg.Amount = amount;

            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        public void SendRemoveFloatingObjectSuccess(MyFloatingObject floatingObject, MyFixedPoint amount)
        {
            System.Diagnostics.Debug.Assert(Sync.IsServer);
            var msg = new RemoveFloatingObjectMsg();
            msg.EntityId = floatingObject.EntityId;
            msg.Amount = amount;

            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        static void RemoveFloatingObjectRequest(ref RemoveFloatingObjectMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(Sync.IsServer);
            MyFloatingObject floatingObject;
            if (MyEntities.TryGetEntityById<MyFloatingObject>(msg.EntityId, out floatingObject))
                MyFloatingObjects.RemoveFloatingObject(floatingObject, msg.Amount);
        }

        static void RemoveFloatingObjectSuccess(ref RemoveFloatingObjectMsg msg, MyNetworkClient sender)
        {
            MyFloatingObject floatingObject;
            if (MyEntities.TryGetEntityById<MyFloatingObject>(msg.EntityId, out floatingObject))
                MyFloatingObjects.RemoveFloatingObject(floatingObject, msg.Amount);
        }

        static void AddFloatingObjectSuccess(ref AddFloatingObjectMsg msg, MyNetworkClient sender)
        {
            MyFloatingObject floatingObject;
            if (MyEntities.TryGetEntityById<MyFloatingObject>(msg.EntityId, out floatingObject))
                MyFloatingObjects.AddFloatingObjectAmount(floatingObject, msg.Amount);
        }
               
        public static void UpdatePosition(MyFloatingObject floatingObject, bool toAll = true, MyNetworkClient receiver = null )
        {
            var m = floatingObject.WorldMatrix;

            FloatingObjectPositionUpdateMsg msg = new FloatingObjectPositionUpdateMsg();
            msg.EntityId = floatingObject.EntityId;
            msg.Forward = m.Forward;
            msg.Up = m.Up;
            msg.Position = m.Translation;
            if (floatingObject.Physics != null)
            {
                msg.LinearVelocity = floatingObject.Physics.LinearVelocity;
                msg.AngularVelocity = floatingObject.Physics.AngularVelocity;
            }

            //PositionUpdateCompressedMsg msg = new PositionUpdateCompressedMsg();
            //msg.EntityId = floatingObject.EntityId;
            //msg.SetWorldMatrix(m);
            //if (floatingObject.Physics != null)
            //{
            //    if (floatingObject.Physics.RigidBody.IsActive)
            //    {
            //        if (floatingObject.Physics.LinearVelocity.Length() > 0.1f || floatingObject.Physics.AngularVelocity.Length() > 0.1f)
            //          msg.SetVelocities(floatingObject.Physics.LinearVelocity, floatingObject.Physics.AngularVelocity);
            //    }
            //}
            if (toAll | receiver == null)
            {
                MySession.Static.SyncLayer.SendMessageToAll(ref msg);
            }
            else
            {
                MySession.Static.SyncLayer.SendMessage(ref msg, receiver.SteamUserId);
            }
        }

        static void OnUpdateCallback(ref FloatingObjectPositionUpdateMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(!Sync.IsServer);

            MyEntity entity;
            if (MyEntities.TryGetEntityById(msg.EntityId, out entity))
            {
                MyFloatingObject floatingObject = entity as MyFloatingObject;
                if (floatingObject != null)
                {
                    MatrixD matrix = MatrixD.CreateWorld(msg.Position, msg.Forward, msg.Up);
                    Vector3D translation = floatingObject.PositionComp.GetPosition();
                    //Quaternion rotation = Quaternion.CreateFromRotationMatrix(floatingObject.WorldMatrix.GetOrientation());
                    Quaternion rotation = Quaternion.Identity;
                    m_rigidBodyList.Clear();
                    
                    Sandbox.Engine.Physics.MyPhysics.GetPenetrationsShape(floatingObject.Physics.RigidBody.GetShape(), ref translation, ref rotation, m_rigidBodyList, Sandbox.Engine.Physics.MyPhysics.NotCollideWithStaticLayer);
                    
                    //if (m_rigidBodyList.Count == 0 || MyPerGameSettings.EnableFloatingObjectsActiveSync)
                    //{
                    //    floatingObject.PositionComp.SetWorldMatrix(matrix, sender);
                    //}

                    if (floatingObject.Physics != null)
                    {
                        if (m_rigidBodyList.Count == 1 && m_rigidBodyList[0].Body == floatingObject.Physics.RigidBody)
                        {
                            floatingObject.PositionComp.SetWorldMatrix(matrix, sender);
                        }

                        floatingObject.Physics.LinearVelocity = msg.LinearVelocity;
                        floatingObject.Physics.AngularVelocity = msg.AngularVelocity;
                    }
                }
            }
        }

        static void OnUpdateCompressedCallback(ref PositionUpdateCompressedMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            if (MyEntities.TryGetEntityById(msg.EntityId, out entity))
            {
                MyFloatingObject floatingObject = entity as MyFloatingObject;
                if (floatingObject != null)
                {
                    MatrixD matrix = msg.GetWorldMatrix();
                    floatingObject.PositionComp.SetWorldMatrix(matrix, sender);
                    if (floatingObject.Physics != null)
                    {
                        Vector3 linearVelocity, angularVelocity;
                        msg.GetVelocities(out  linearVelocity, out angularVelocity);
                        floatingObject.Physics.LinearVelocity = linearVelocity;
                        floatingObject.Physics.AngularVelocity = angularVelocity;
                    }
                }
            }
        }

        public void OnCreateFloatingObjects(List<MyFloatingObject> floatingObjects)
        {
            if (floatingObjects.Count == 0)
                return;

            foreach (var floatingObject in floatingObjects)
            {
                int key = (int)floatingObject.Item.Content.TypeId.GetHashCode() ^ floatingObject.Item.Content.SubtypeId.GetHashCode();
                FloatingObjectsData data;
                if (!m_sortingMap.TryGetValue(key, out data))
                {
                    data = new FloatingObjectsData();
                    data.Instances = new List<MyFloatingObject>();
                    data.TypeId = new DefinitionIdBlit(floatingObject.Item.Content.TypeId, floatingObject.Item.Content.SubtypeId);
                    m_sortingMap[key] = data;
                }

                data.Instances.Add(floatingObject);
            }

            FloatingObjectsCreateMsg msg = new FloatingObjectsCreateMsg();

            msg.FloatingObjects = new List<CreatedFloatingObject>();

            foreach (FloatingObjectsData data in m_sortingMap.Values)
            {
                if (data.Instances.Count > 0)
                {
                    CreatedFloatingObject createMsg = new CreatedFloatingObject();
                    createMsg.TypeId = data.TypeId;
                    createMsg.Instances = new List<CreatedFloatingObjectInstance>();

                    foreach (var floatingObject in data.Instances)
                    {
                        // Object may be reduced/damaged meanwhile?
                        if (floatingObject.Item.Amount <= 0)
                            continue;

                        CreatedFloatingObjectInstance instanceData = new CreatedFloatingObjectInstance();
                        instanceData.Location = new FloatingObjectPositionUpdateMsg();
                        System.Diagnostics.Debug.Assert(floatingObject.Item.Amount > 0, "Floating object amount must be > 0");
                        instanceData.Amount = floatingObject.Item.Amount;
                        instanceData.Location.Position = floatingObject.PositionComp.GetPosition();
                        instanceData.Location.Forward = floatingObject.WorldMatrix.Forward;
                        instanceData.Location.Up = floatingObject.WorldMatrix.Up;
                        instanceData.Location.EntityId = floatingObject.EntityId;

                        if (floatingObject.Physics != null)
                        {
                            instanceData.Location.LinearVelocity = floatingObject.Physics.LinearVelocity;
                            instanceData.Location.AngularVelocity = floatingObject.Physics.AngularVelocity;
                        }

                        createMsg.Instances.Add(instanceData);
                    }

                    msg.FloatingObjects.Add(createMsg);
                }


            }

            foreach (var data in m_sortingMap.Values)
            {
                data.Instances.Clear();
            }

            MySession.Static.SyncLayer.SendMessageToServer(ref msg);


            //foreach (var floatingObject in floatingObjects)
            //{
            //    MySyncCreate.SendEntityCreated(floatingObject.GetObjectBuilder());
            //}
        }

        static void OnCreateFloatingObjectsCallback(ref FloatingObjectsCreateMsg msg, MyNetworkClient sender)
        {
            //MySandboxGame.Log.WriteLine("FloatingObjectsCreateMsg: " + msg.FloatingObjects.Count);

            foreach (var floatingObject in msg.FloatingObjects)
            {
                foreach (var instance in floatingObject.Instances)
                {
                    System.Diagnostics.Debug.Assert(instance.Amount > 0);

                    if (instance.Amount <= 0)
                        continue;

                    if (MyEntities.EntityExists(instance.Location.EntityId))
                        continue;

                    var objectBuilder = new MyObjectBuilder_FloatingObject();
                    objectBuilder.Item = new MyObjectBuilder_InventoryItem();
                    objectBuilder.Item.Amount = instance.Amount;
                    objectBuilder.Item.Content = MyObjectBuilderSerializer.CreateNewObject(((MyDefinitionId)floatingObject.TypeId).TypeId, ((MyDefinitionId)floatingObject.TypeId).SubtypeName);
                    objectBuilder.EntityId = instance.Location.EntityId;
                    objectBuilder.PositionAndOrientation = new MyPositionAndOrientation(instance.Location.Position, instance.Location.Forward, instance.Location.Up);
                    objectBuilder.PersistentFlags = MyPersistentEntityFlags2.InScene | MyPersistentEntityFlags2.Enabled | MyPersistentEntityFlags2.CastShadows;

                    MyFloatingObject floatingObjectAdded = (MyFloatingObject)MyEntities.CreateFromObjectBuilderAndAdd(objectBuilder);
                    if (floatingObjectAdded.Physics != null)
                    {
                        floatingObjectAdded.Physics.LinearVelocity = instance.Location.LinearVelocity;
                        floatingObjectAdded.Physics.AngularVelocity = instance.Location.AngularVelocity;
                    }
                }
            }
            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
            }
        }

        public static void RequestPositionFromServer(MyFloatingObject floatingObject)
        {
            FloatingObjectRequestPosMsg msg = new FloatingObjectRequestPosMsg();
            msg.EntityId = floatingObject.EntityId;
            MySession.Static.SyncLayer.SendMessageToServer(ref msg);
        }

        private static void OnFloatingObjectsRequestPosCallback(ref FloatingObjectRequestPosMsg msg, MyNetworkClient sender)
        {
            MyFloatingObject floatingObject;
            MyEntity entity = null;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            
            floatingObject = entity as MyFloatingObject;
            if (floatingObject != null)
            {

                MatrixD matrix = floatingObject.WorldMatrix;

                Vector3D translation = matrix.Translation;
                Quaternion rotation =  Quaternion.CreateFromRotationMatrix(matrix.GetOrientation());

                if (MyPerGameSettings.EnableFloatingObjectsActiveSync)
                {
                    m_rigidBodyList.Clear();
                    Sandbox.Engine.Physics.MyPhysics.GetPenetrationsShape(floatingObject.Physics.RigidBody.GetShape(), ref translation, ref rotation, m_rigidBodyList, Sandbox.Engine.Physics.MyPhysics.NotCollideWithStaticLayer);

                    tmp_list.Clear();

                    foreach (var body in m_rigidBodyList)
                    {
                        var physicsBody = body.Body.UserObject as MyPhysicsBody;
                        if (physicsBody != null)
                        {
                            var collidingFloatingObject = physicsBody.Entity as MyFloatingObject;
                            if (collidingFloatingObject != null && !tmp_list.Contains(collidingFloatingObject))
                            {
                                tmp_list.Add(collidingFloatingObject);
                                if (tmp_list.Count > MAX_SYNC_ON_REQUEST)
                                    break;
                            }
                        }
                    }

                    foreach (var floatOb in tmp_list)
                    {
                        UpdatePosition(floatOb, false, sender);
                    }
                }
                
                UpdatePosition(floatingObject, false, sender);
            }
            else
            {
                //System.Diagnostics.Debug.Fail("Requested floating object wasn't found!");
            }
        }

        public void SendMakeStableBatchReq(List<long> entities)
        {  
            MakeStableBatchReqMsg msg = new MakeStableBatchReqMsg();
            msg.Entities = new List<long>(entities);
            MySession.Static.SyncLayer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void OnMakeStableBatchReq(ref MakeStableBatchReqMsg msg, MyNetworkClient sender)
        {
            var character = sender.FirstPlayer.Character;
            if (character == null)
                return;
            m_floatingObjects.ProcessStableBatchReq(msg.Entities, character, m_tmpStableData, m_tmpNonStableIds);

            if (m_tmpStableData.Count > 0)
            {
                var totalSize = m_tmpStableData.Count * RelativeEntityDataSize + MakeStableBatchEntityDataSize;
                if (totalSize > 1000)
                {
                    var desired = 1000 / RelativeEntityDataSize;
                    var tmpList = new List<MakeStableEntityData>();
                    for (int i = 0; i < m_tmpStableData.Count; i += desired)
                    {
                        int count = Math.Min(i + desired, m_tmpStableData.Count);
                        for (int j = i; j < count; ++j)
                            tmpList.Add(m_tmpStableData[j]);
                        var partMsg = new MakeStableBatchMsg() { StableObjects = tmpList };
                        Sync.Layer.SendMessage(ref partMsg, sender.SteamUserId, MyTransportMessageEnum.Success);
                        tmpList.Clear();
                    }                   
                }
                else
                {
                    var batchMsg = new MakeStableBatchMsg() { StableObjects = m_tmpStableData };
                    Sync.Layer.SendMessage(ref batchMsg, sender.SteamUserId, MyTransportMessageEnum.Success);
                }
                m_tmpStableData.Clear();
            }
          
            if (m_tmpNonStableIds.Count > 0)
            {
                msg = new MakeStableBatchReqMsg() { Entities = m_tmpNonStableIds };
                Sync.Layer.SendMessage(ref msg, sender.SteamUserId, MyTransportMessageEnum.Failure);
                m_tmpNonStableIds.Clear();
            }
        }

        static void OnMakeStableBatchFailure(ref MakeStableBatchReqMsg msg, MyNetworkClient sender)
        {
            m_floatingObjects.MakeStableFailed(msg.Entities);
        }

        static void OnMakeStableBatchSuccess(ref MakeStableBatchMsg msg, MyNetworkClient sender)
        {
            m_floatingObjects.MakeStable(msg.StableObjects);
        }

        public void SendMakeUnstable(List<long> objects)
        {
            var msg = new MakeUnstableBatchMsg()
            {
                Entities = objects,
            };
            MySession.Static.SyncLayer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        private static void OnMakeUnstableBatchSuccess(ref MakeUnstableBatchMsg msg, MyNetworkClient sender)
        {
            m_floatingObjects.MakeUnstable(msg.Entities);
        }
    }
}
