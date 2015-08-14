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

#endregion

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    class MySyncFloatingObjects
    {
        MyFloatingObjects m_floatingObjects;
        static System.Collections.Generic.List<Havok.HkBodyCollision> m_rigidBodyList = new System.Collections.Generic.List<Havok.HkBodyCollision>();

        struct FloatingObjectsData
        {
            public DefinitionIdBlit TypeId;
            public List<MyFloatingObject> Instances;
        }
        Dictionary<int, FloatingObjectsData> m_sortingMap = new Dictionary<int, FloatingObjectsData>();

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


        public static int PositionUpdateCompressedMsgSize = BlitSerializer<PositionUpdateCompressedMsg>.StructSize;


        static MySyncFloatingObjects()
        {
            MySyncLayer.RegisterMessage<FloatingObjectPositionUpdateMsg>(OnUpdateCallback, MyMessagePermissions.FromServer);
            MySyncLayer.RegisterMessage<PositionUpdateCompressedMsg>(OnUpdateCompressedCallback, MyMessagePermissions.FromServer);
            MySyncLayer.RegisterMessage<RemoveFloatingObjectMsg>(RemoveFloatingObjectSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<FloatingObjectsCreateMsg>(OnCreateFloatingObjectsCallback, MyMessagePermissions.Any, MyTransportMessageEnum.Request, new FloatingObjectsSerializer());
        }

        public MySyncFloatingObjects()
        {
        }

        public void OnRemoveFloatingObject(MyFloatingObject floatingObject, MyFixedPoint amount)
        {
            System.Diagnostics.Debug.Assert(Sync.IsServer);
            var msg = new RemoveFloatingObjectMsg();
            msg.EntityId = floatingObject.EntityId;
            msg.Amount = amount;

            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        static void RemoveFloatingObjectSuccess(ref RemoveFloatingObjectMsg msg, MyNetworkClient sender)
        {
            MyFloatingObject floatingObject;
            if (MyEntities.TryGetEntityById<MyFloatingObject>(msg.EntityId, out floatingObject))
                MyFloatingObjects.RemoveFloatingObject(floatingObject, msg.Amount);
        }

        public void UpdatePosition(MyFloatingObject floatingObject)
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

            MySession.Static.SyncLayer.SendMessageToAll(ref msg);
        }

        static void OnUpdateCallback(ref FloatingObjectPositionUpdateMsg msg, MyNetworkClient sender)
        {
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
                    Sandbox.Engine.Physics.MyPhysics.GetPenetrationsShape(floatingObject.Physics.RigidBody.GetShape(), ref translation, ref rotation, m_rigidBodyList, 0);
                    if (m_rigidBodyList.Count == 0)
                    {
                        floatingObject.PositionComp.SetWorldMatrix(matrix, sender);
                    }
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

            MySession.Static.SyncLayer.SendMessageToAll(ref msg);


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
        }
    }
}
